using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

public class GenerateTaskTemplateCommandHandler : IRequestHandler<GenerateTaskTemplateCommand, int>
{
    private readonly IEventTaskRepository _taskRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateTaskTemplateCommandHandler(
        IEventTaskRepository taskRepository,
        IWeddingEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GenerateTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
            throw new NotFoundException("Event", request.EventId);

        var canManage = weddingEvent.ManagingPlannerId == request.UserId
            || weddingEvent.CreatedById == request.UserId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, request.UserId, cancellationToken);

        if (!canManage)
            throw new ForbiddenAccessException("You do not have permission to generate tasks for this event.");

        if (request.SkipIfTasksExist && await _taskRepository.AnyByEventIdAsync(request.EventId, cancellationToken))
            return 0;

        var weddingDate = weddingEvent.EventDate.Date;
        var template = WeddingTaskTemplate.Entries;
        var now = DateTime.UtcNow;
        var daysUntilWedding = Math.Max(1, (weddingDate - now.Date).Days);

        var includedIndices = new List<int>();
        for (var i = 0; i < template.Count; i++)
        {
            if (WeddingTaskScheduleCalculator.ShouldIncludeTemplateEntry(template[i].DueDaysBeforeWedding, daysUntilWedding))
            {
                includedIndices.Add(i);
            }
        }

        if (!includedIndices.Contains(template.Count - 1))
        {
            includedIndices.Add(template.Count - 1);
            includedIndices.Sort();
        }

        var indexToTask = new Dictionary<int, EventTask>();
        var tasksInOrder = new List<EventTask>();

        foreach (var index in includedIndices)
        {
            var entry = template[index];
            var window = WeddingTaskScheduleCalculator.ComputeScheduleDates(
                weddingDate,
                now,
                entry.StartDaysBeforeWedding,
                entry.DueDaysBeforeWedding);

            var task = new EventTask
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Title = entry.Title,
                Status = DomainTaskStatus.ToDo,
                StartDate = window.StartDate,
                DueDate = window.DueDate,
                CreatedAt = now,
                UpdatedAt = now
            };

            indexToTask[index] = task;
            tasksInOrder.Add(task);
            await _taskRepository.AddAsync(task, cancellationToken);
        }

        foreach (var index in includedIndices)
        {
            var dependsOnIndex = template[index].DependsOnTemplateIndex;
            if (dependsOnIndex is null)
            {
                continue;
            }

            var task = indexToTask[index];
            if (indexToTask.TryGetValue(dependsOnIndex.Value, out var dependency))
            {
                task.DependsOnTaskId = dependency.Id;
                continue;
            }

            var fallbackIndex = includedIndices.LastOrDefault(i => i <= dependsOnIndex.Value);
            if (indexToTask.TryGetValue(fallbackIndex, out var fallbackDep))
            {
                task.DependsOnTaskId = fallbackDep.Id;
            }
        }

        var orderedTemplateTasks = includedIndices.Select(i => indexToTask[i]).ToList();
        WeddingTaskScheduleCalculator.EnforceDependencyOrderByGraph(orderedTemplateTasks, weddingDate, orderedTemplateTasks);

        foreach (var task in orderedTemplateTasks)
        {
            task.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return includedIndices.Count;
    }
}

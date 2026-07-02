using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateDiscoveryTasks;

public class GenerateDiscoveryTasksCommandHandler : IRequestHandler<GenerateDiscoveryTasksCommand, int>
{
    private readonly IEventTaskRepository _taskRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateDiscoveryTasksCommandHandler(
        IEventTaskRepository taskRepository,
        IWeddingEventRepository eventRepository,
        ICollaborationService collaborationService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _eventRepository = eventRepository;
        _collaborationService = collaborationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GenerateDiscoveryTasksCommand request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        var canManage = weddingEvent.ManagingPlannerId == request.UserId
            || weddingEvent.CreatedById == request.UserId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, request.UserId, cancellationToken);

        if (!canManage)
        {
            throw new ForbiddenAccessException("You do not have permission to generate tasks for this event.");
        }

        if (weddingEvent.TaskPlanPhase == TaskPlanPhase.Full)
        {
            return 0;
        }

        if (request.SkipIfDiscoveryExists && weddingEvent.TaskPlanPhase == TaskPlanPhase.Discovery)
        {
            return 0;
        }

        if (request.SkipIfDiscoveryExists && await _taskRepository.AnyByEventIdAsync(request.EventId, cancellationToken))
        {
            return 0;
        }

        var template = WeddingDiscoveryTaskTemplate.Entries;
        var now = DateTime.UtcNow;
        var planStart = now.Date;
        var indexToTask = new Dictionary<int, EventTask>();

        foreach (var (entry, index) in template.Select((e, i) => (e, i)))
        {
            var start = planStart.AddDays(entry.StartOffsetDays);
            var due = planStart.AddDays(Math.Max(entry.DueOffsetDays, entry.StartOffsetDays + 1));

            var task = new EventTask
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Title = entry.Title,
                Status = DomainTaskStatus.ToDo,
                StartDate = start,
                DueDate = due,
                CreatedAt = now,
                UpdatedAt = now
            };

            indexToTask[index] = task;
            await _taskRepository.AddAsync(task, cancellationToken);
        }

        foreach (var (entry, index) in template.Select((e, i) => (e, i)))
        {
            if (entry.DependsOnTemplateIndex is null)
            {
                continue;
            }

            if (indexToTask.TryGetValue(index, out var task)
                && indexToTask.TryGetValue(entry.DependsOnTemplateIndex.Value, out var dependency))
            {
                task.DependsOnTaskId = dependency.Id;
            }
        }

        weddingEvent.TaskPlanPhase = TaskPlanPhase.Discovery;
        if (weddingEvent.EventLifecycleStage == EventLifecycleStage.Lead)
        {
            weddingEvent.EventLifecycleStage = EventLifecycleStage.Onboarding;
        }

        weddingEvent.UpdatedAt = now;
        _eventRepository.Update(weddingEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);

        return template.Count;
    }
}

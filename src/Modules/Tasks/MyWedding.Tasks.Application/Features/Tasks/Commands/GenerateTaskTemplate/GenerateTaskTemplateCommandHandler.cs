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
        var taskIds = new Guid[template.Count];
        var now = DateTime.UtcNow;

        for (var i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            taskIds[i] = Guid.NewGuid();
            var task = new EventTask
            {
                Id = taskIds[i],
                EventId = request.EventId,
                Title = entry.Title,
                Status = DomainTaskStatus.ToDo,
                StartDate = weddingDate.AddDays(-entry.StartDaysBeforeWedding),
                DueDate = weddingDate.AddDays(-entry.DueDaysBeforeWedding),
                CreatedAt = now,
                UpdatedAt = now
            };
            await _taskRepository.AddAsync(task, cancellationToken);
        }

        for (var i = 0; i < template.Count; i++)
        {
            var dependsOnIndex = template[i].DependsOnTemplateIndex;
            if (dependsOnIndex is null)
                continue;

            var task = await _taskRepository.GetByIdAsync(taskIds[i], cancellationToken);
            if (task is null)
                continue;

            task.DependsOnTaskId = taskIds[dependsOnIndex.Value];
            task.UpdatedAt = now;
            _taskRepository.Update(task);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return template.Count;
    }
}

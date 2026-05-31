using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.RealignEventTaskSchedule;

public class RealignEventTaskScheduleCommandHandler
    : IRequestHandler<RealignEventTaskScheduleCommand, RealignEventTaskScheduleResult>
{
    private readonly IEventTaskRepository _taskRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RealignEventTaskScheduleCommandHandler(
        IEventTaskRepository taskRepository,
        IWeddingEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RealignEventTaskScheduleResult> Handle(
        RealignEventTaskScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        var canManage = weddingEvent.ManagingPlannerId == request.UserId
            || weddingEvent.CreatedById == request.UserId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, request.UserId!, cancellationToken);

        if (!canManage)
        {
            throw new ForbiddenAccessException("You do not have permission to realign tasks for this event.");
        }

        var allTasks = (await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken)).ToList();
        if (allTasks.Count == 0)
        {
            return new RealignEventTaskScheduleResult(0, 0);
        }

        var template = WeddingTaskTemplate.Entries;
        var weddingDate = weddingEvent.EventDate.Date;
        var planStart = DateTime.UtcNow;
        var daysUntilWedding = Math.Max(1, (weddingDate - planStart.Date).Days);

        var tasksByTemplateIndex = new EventTask?[template.Count];
        var unmatched = new List<EventTask>();

        foreach (var task in allTasks)
        {
            var index = WeddingTaskScheduleCalculator.FindTemplateIndexByTitle(task.Title);
            if (index is null || tasksByTemplateIndex[index.Value] is not null)
            {
                unmatched.Add(task);
                continue;
            }

            tasksByTemplateIndex[index.Value] = task;
        }

        var updated = 0;
        var skipped = 0;
        var now = DateTime.UtcNow;

        for (var i = 0; i < template.Count; i++)
        {
            var task = tasksByTemplateIndex[i];
            if (task is null)
            {
                continue;
            }

            if (task.Status == DomainTaskStatus.Completed)
            {
                skipped++;
                continue;
            }

            if (!WeddingTaskScheduleCalculator.ShouldIncludeTemplateEntry(
                    template[i].DueDaysBeforeWedding,
                    daysUntilWedding))
            {
                skipped++;
                continue;
            }

            var window = WeddingTaskScheduleCalculator.ComputeScheduleDates(
                weddingDate,
                planStart,
                template[i].StartDaysBeforeWedding,
                template[i].DueDaysBeforeWedding);

            task.StartDate = window.StartDate;
            task.DueDate = window.DueDate;
            task.UpdatedAt = now;
            _taskRepository.Update(task);
            updated++;
        }

        var templateMatched = tasksByTemplateIndex
            .Where(t => t is not null && t.Status != DomainTaskStatus.Completed)
            .Cast<EventTask>()
            .ToList();

        if (templateMatched.Count > 0)
        {
            WeddingTaskScheduleCalculator.EnforceDependencyOrderByGraph(
                templateMatched,
                weddingDate,
                allTasks);

            foreach (var task in templateMatched)
            {
                task.UpdatedAt = now;
                _taskRepository.Update(task);
            }
        }

        foreach (var task in unmatched.Where(t => t.Status != DomainTaskStatus.Completed))
        {
            if (task.DueDate.HasValue && task.DueDate.Value.Date >= planStart.Date)
            {
                skipped++;
                continue;
            }

            task.StartDate = planStart.Date;
            task.DueDate = planStart.Date.AddDays(Math.Min(7, Math.Max(1, daysUntilWedding / 14)));
            if (task.DueDate > weddingDate)
            {
                task.DueDate = weddingDate;
            }

            task.UpdatedAt = now;
            _taskRepository.Update(task);
            updated++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new RealignEventTaskScheduleResult(updated, skipped);
    }
}

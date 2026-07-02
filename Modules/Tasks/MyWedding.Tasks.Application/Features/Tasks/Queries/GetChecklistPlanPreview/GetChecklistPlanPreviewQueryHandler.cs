using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.Entities;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetChecklistPlanPreview;

public class GetChecklistPlanPreviewQueryHandler : IRequestHandler<GetChecklistPlanPreviewQuery, ChecklistPlanPreviewDto>
{
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IEventTaskRepository _taskRepository;

    public GetChecklistPlanPreviewQueryHandler(
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        IEventTaskRepository taskRepository)
    {
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _taskRepository = taskRepository;
    }

    public async Task<ChecklistPlanPreviewDto> Handle(
        GetChecklistPlanPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        await EnsureCanAccessAsync(request.EventId, request.UserId, weddingEvent.ManagingPlannerId, cancellationToken);

        var existingTasks = (await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken)).ToList();
        var discoveryCount = existingTasks.Count(t => ChecklistTemplatePlanner.IsDiscoveryTaskTitle(t.Title));

        var preview = ChecklistTemplatePlanner.BuildFullTemplatePreview(
            weddingEvent.EventDate,
            DateTime.UtcNow);

        var briefCompletion = CalculateBriefCompletionPercent(weddingEvent);

        return new ChecklistPlanPreviewDto(
            weddingEvent.Id,
            weddingEvent.EventName,
            weddingEvent.EventDate,
            weddingEvent.TaskPlanPhase.ToString(),
            weddingEvent.BriefCompletedAt.HasValue || briefCompletion >= 80,
            briefCompletion,
            discoveryCount,
            existingTasks.Count,
            weddingEvent.TaskPlanPhase != TaskPlanPhase.Full,
            preview.Select(p => new ChecklistPreviewTaskDto(
                p.TemplateIndex,
                p.Title,
                p.StartDate,
                p.DueDate,
                IncludedByDefault: true,
                ExcludedByAi: false)).ToList(),
            new EventBriefSummaryDto(
                weddingEvent.EstimatedGuestCount,
                weddingEvent.GuestCountMax,
                weddingEvent.WeddingStyle,
                weddingEvent.VenuePreference,
                weddingEvent.MustHavesNotes,
                weddingEvent.ServicesAlreadyBooked,
                weddingEvent.CulturalOrReligiousNotes
            )
        );
    }

    private static int CalculateBriefCompletionPercent(WeddingEvent weddingEvent)
    {
        var score = 0;
        if (weddingEvent.EstimatedGuestCount is > 0) score += 20;
        if (!string.IsNullOrWhiteSpace(weddingEvent.WeddingStyle)) score += 20;
        if (!string.IsNullOrWhiteSpace(weddingEvent.VenuePreference)) score += 15;
        if (!string.IsNullOrWhiteSpace(weddingEvent.MustHavesNotes)) score += 25;
        if (weddingEvent.TotalBudget > 0) score += 10;
        if (!string.IsNullOrWhiteSpace(weddingEvent.ServicesAlreadyBooked)) score += 10;
        return Math.Min(100, score);
    }

    private async Task EnsureCanAccessAsync(
        Guid eventId,
        string userId,
        string? managingPlannerId,
        CancellationToken cancellationToken)
    {
        if (managingPlannerId == userId)
        {
            return;
        }

        var isOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId, cancellationToken);
        if (!isOrganizer)
        {
            var canManage = await _eventRepository.IsManagedByPlannerAsync(eventId, userId, cancellationToken);
            if (!canManage)
            {
                throw new ForbiddenAccessException("You do not have access to this checklist preview.");
            }
        }
    }
}

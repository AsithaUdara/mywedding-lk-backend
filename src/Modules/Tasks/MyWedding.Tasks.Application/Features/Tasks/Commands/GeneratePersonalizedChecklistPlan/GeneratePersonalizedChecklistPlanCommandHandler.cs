using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GeneratePersonalizedChecklistPlan;

public class GeneratePersonalizedChecklistPlanCommandHandler
    : IRequestHandler<GeneratePersonalizedChecklistPlanCommand, PersonalizedChecklistPlanDto>
{
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IAiCopilotService _aiCopilotService;

    public GeneratePersonalizedChecklistPlanCommandHandler(
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        IAiCopilotService aiCopilotService)
    {
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _aiCopilotService = aiCopilotService;
    }

    public async Task<PersonalizedChecklistPlanDto> Handle(
        GeneratePersonalizedChecklistPlanCommand request,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        await EnsureCanManageAsync(request.EventId, request.UserId, weddingEvent.ManagingPlannerId, cancellationToken);

        var preview = ChecklistTemplatePlanner.BuildFullTemplatePreview(
            weddingEvent.EventDate,
            DateTime.UtcNow);

        var aiResult = await _aiCopilotService.GeneratePersonalizedChecklistPlanAsync(
            new PersonalizedChecklistPlanRequest(
                weddingEvent.Id,
                weddingEvent.EventName,
                DateOnly.FromDateTime(weddingEvent.EventDate),
                weddingEvent.EstimatedGuestCount,
                weddingEvent.GuestCountMax,
                weddingEvent.WeddingStyle,
                weddingEvent.VenuePreference,
                weddingEvent.MustHavesNotes,
                weddingEvent.ServicesAlreadyBooked,
                weddingEvent.CulturalOrReligiousNotes,
                weddingEvent.TotalBudget > 0 ? weddingEvent.TotalBudget : null,
                request.MeetingNotesOrTranscript,
                preview.Select(p => p.Title).ToList()),
            cancellationToken);

        var excludeSet = new HashSet<string>(aiResult.ExcludeTemplateTitles, StringComparer.OrdinalIgnoreCase);

        var lines = preview.Select(p => new ChecklistPreviewLineDto(
            p.TemplateIndex,
            p.Title,
            IncludedByDefault: true,
            ExcludedByPlan: excludeSet.Contains(p.Title))).ToList();

        var additional = aiResult.AdditionalTasks
            .Select(t => new ProposedAdditionalTaskDto(t.Title, t.Description, t.Priority))
            .ToList();

        return new PersonalizedChecklistPlanDto(
            aiResult.ExecutiveSummary,
            aiResult.ExcludeTemplateTitles.ToList(),
            additional,
            lines,
            aiResult.IsSimulated);
    }

    private async Task EnsureCanManageAsync(
        Guid eventId,
        string userId,
        string? managingPlannerId,
        CancellationToken cancellationToken)
    {
        var canManage = managingPlannerId == userId
            || await _eventRepository.IsManagedByPlannerAsync(eventId, userId, cancellationToken);

        if (canManage)
        {
            return;
        }

        var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId, cancellationToken);
        if (organizer is null || organizer.PermissionLevel == Domain.Enums.PermissionLevel.Viewer)
        {
            throw new ForbiddenAccessException("You do not have permission to personalize this checklist.");
        }
    }
}

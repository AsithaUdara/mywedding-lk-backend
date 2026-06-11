using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Events.Application.Features.EventBrief.UpdateEventBrief;

public class UpdateEventBriefCommandHandler : IRequestHandler<UpdateEventBriefCommand, EventBriefDto>
{
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventBriefCommandHandler(
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventBriefDto> Handle(UpdateEventBriefCommand request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        await EnsureCanManageAsync(request.EventId, request.UserId, weddingEvent, cancellationToken);

        if (request.EstimatedGuestCount is < 0 or > 10000)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "estimatedGuestCount", new[] { "Guest count must be between 1 and 10,000." } }
            });
        }

        if (request.GuestCountMax is < 0 or > 10000)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "guestCountMax", new[] { "Maximum guest count must be between 1 and 10,000." } }
            });
        }

        weddingEvent.EstimatedGuestCount = request.EstimatedGuestCount;
        weddingEvent.GuestCountMax = request.GuestCountMax;
        weddingEvent.WeddingStyle = Normalize(request.WeddingStyle);
        weddingEvent.VenuePreference = Normalize(request.VenuePreference);
        weddingEvent.MustHavesNotes = Normalize(request.MustHavesNotes);
        weddingEvent.ServicesAlreadyBooked = Normalize(request.ServicesAlreadyBooked);
        weddingEvent.CulturalOrReligiousNotes = Normalize(request.CulturalOrReligiousNotes);

        if (request.MarkBriefComplete)
        {
            weddingEvent.BriefCompletedAt = DateTime.UtcNow;
            if (weddingEvent.EventLifecycleStage == EventLifecycleStage.Lead)
            {
                weddingEvent.EventLifecycleStage = EventLifecycleStage.Onboarding;
            }
        }

        weddingEvent.UpdatedAt = DateTime.UtcNow;
        _eventRepository.Update(weddingEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EventBriefMapper.ToDto(weddingEvent);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureCanManageAsync(
        Guid eventId,
        string userId,
        Domain.Entities.WeddingEvent weddingEvent,
        CancellationToken cancellationToken)
    {
        var canManageViaPlanner = weddingEvent.ManagingPlannerId == userId
            || weddingEvent.CreatedById == userId
            || await _eventRepository.IsManagedByPlannerAsync(eventId, userId, cancellationToken);

        if (canManageViaPlanner)
        {
            return;
        }

        var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId, cancellationToken);
        if (organizer is null || organizer.PermissionLevel == PermissionLevel.Viewer)
        {
            throw new ForbiddenAccessException("You do not have permission to update this event brief.");
        }
    }
}

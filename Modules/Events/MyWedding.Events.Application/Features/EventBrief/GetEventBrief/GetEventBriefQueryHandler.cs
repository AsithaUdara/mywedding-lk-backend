using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.Events.Application.Features.EventBrief.GetEventBrief;

public class GetEventBriefQueryHandler : IRequestHandler<GetEventBriefQuery, EventBriefDto>
{
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;

    public GetEventBriefQueryHandler(
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository)
    {
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
    }

    public async Task<EventBriefDto> Handle(GetEventBriefQuery request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken)
            ?? await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        await EnsureCanAccessAsync(request.EventId, request.UserId, weddingEvent.ManagingPlannerId, cancellationToken);

        return EventBriefMapper.ToDto(weddingEvent);
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
                throw new ForbiddenAccessException("You do not have access to this event brief.");
            }
        }
    }
}

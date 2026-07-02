using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Events.Application.Features.Events.Queries.GetEventById;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IWeddingEventRepository _weddingEventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingPlannerProfileReader _plannerProfileReader;

    public GetEventByIdQueryHandler(
        IWeddingEventRepository weddingEventRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingPlannerProfileReader plannerProfileReader)
    {
        _weddingEventRepository = weddingEventRepository;
        _organizerRepository = organizerRepository;
        _plannerProfileReader = plannerProfileReader;
    }

    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _weddingEventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (weddingEvent is null)
        {
            return null;
        }

        var isOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(
            request.EventId,
            request.UserId,
            cancellationToken);

        if (!isOrganizer)
        {
            throw new ForbiddenAccessException("You do not have permission to view this event.");
        }

        var plannerBranding = await _plannerProfileReader.GetByEventIdAsync(request.EventId, cancellationToken);

        EventPlannerBrandingDto? brandingDto = plannerBranding is null
            ? null
            : new EventPlannerBrandingDto(
                plannerBranding.BusinessName,
                plannerBranding.DisplayName,
                plannerBranding.AgencyLogoUrl,
                plannerBranding.IsWhiteLabeled);

        return new EventDto(
            weddingEvent.Id,
            weddingEvent.EventName,
            weddingEvent.EventDate,
            weddingEvent.CreatedById,
            weddingEvent.TotalBudget,
            PlannerBranding: brandingDto);
    }
}

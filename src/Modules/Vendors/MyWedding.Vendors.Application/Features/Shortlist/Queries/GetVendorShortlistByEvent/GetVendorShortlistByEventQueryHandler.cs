using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Queries.GetVendorShortlistByEvent;

public class GetVendorShortlistByEventQueryHandler
    : IRequestHandler<GetVendorShortlistByEventQuery, IReadOnlyList<VendorShortlistItemDto>>
{
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;

    public GetVendorShortlistByEventQueryHandler(
        IVendorShortlistRepository shortlistRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository)
    {
        _shortlistRepository = shortlistRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyList<VendorShortlistItemDto>> Handle(
        GetVendorShortlistByEventQuery request,
        CancellationToken cancellationToken)
    {
        var isPlanner = await _eventRepository.IsManagedByPlannerAsync(
            request.EventId, request.UserId, cancellationToken);
        var organizer = await _organizerRepository.GetOrganizerAsync(
            request.EventId, request.UserId, cancellationToken);

        if (!isPlanner && organizer is null)
            throw new ForbiddenAccessException();

        var items = await _shortlistRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        if (!isPlanner && organizer is not null)
        {
            items = items
                .Where(i => i.Status != VendorShortlistItemStatus.Draft)
                .ToList();
        }

        return items.Select(i => new VendorShortlistItemDto(
            i.Id,
            i.VendorServiceId,
            i.VendorService?.ServiceName,
            i.VendorService?.Vendor?.BusinessName,
            i.CategoryLabel,
            i.PlannerNotes,
            i.Status.ToString(),
            i.ProposedAmount,
            i.ServiceDate,
            i.VendorBookingId,
            i.SentToClientAt,
            i.ClientApprovedAt)).ToList();
    }
}

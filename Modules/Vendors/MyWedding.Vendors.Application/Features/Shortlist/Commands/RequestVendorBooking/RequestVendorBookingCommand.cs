using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.RequestVendorBooking;

public class RequestVendorBookingCommand : IRequest<Guid>
{
    public Guid EventId { get; set; }
    public Guid ShortlistItemId { get; set; }
    public required string UserId { get; set; }
}

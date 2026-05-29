using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.DeclineVendorBooking;

public class DeclineVendorBookingCommand : IRequest<Unit>
{
    public Guid BookingId { get; init; }
    public required string VendorUserId { get; init; }
}

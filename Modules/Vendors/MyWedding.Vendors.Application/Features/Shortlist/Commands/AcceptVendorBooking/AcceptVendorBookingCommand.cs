using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.AcceptVendorBooking;

public class AcceptVendorBookingCommand : IRequest<Unit>
{
    public Guid BookingId { get; set; }
    public required string VendorUserId { get; set; }
}

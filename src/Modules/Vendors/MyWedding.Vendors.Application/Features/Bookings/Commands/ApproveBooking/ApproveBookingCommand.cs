using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.ApproveBooking;

public class ApproveBookingCommand : IRequest<bool>
{
    public Guid BookingId { get; init; }
    public required string UserId { get; init; }
}

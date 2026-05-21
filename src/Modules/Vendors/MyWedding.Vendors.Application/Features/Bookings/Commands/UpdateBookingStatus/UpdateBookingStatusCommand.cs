using MediatR;
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.UpdateBookingStatus
{
    public class UpdateBookingStatusCommand : IRequest<bool>
    {
        public required Guid BookingId { get; init; }
        public required BookingStatus NewStatus { get; init; }
        public required string VendorUserId { get; init; }
    }
}

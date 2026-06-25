using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommand : IRequest<Guid>
    {
        public Guid EventId { get; init; }
        public Guid ServiceId { get; init; } // The specific vendor service being booked
        public decimal FinalAmount { get; init; } // The agreed price
        public DateTime ServiceDate { get; init; }
        public required string UserId { get; init; } // The ID of the user making the booking
    }
}

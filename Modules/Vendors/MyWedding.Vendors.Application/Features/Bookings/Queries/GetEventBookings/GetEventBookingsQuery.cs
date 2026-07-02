using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetEventBookings
{
    public class GetEventBookingsQuery : IRequest<List<EventBookingDto>>
    {
        public required Guid EventId { get; init; }
        public required string UserId { get; init; }
    }
}

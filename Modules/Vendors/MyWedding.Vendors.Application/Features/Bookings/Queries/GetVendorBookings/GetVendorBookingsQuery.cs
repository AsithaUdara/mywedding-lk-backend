using MediatR;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetVendorBookings
{
    public class GetVendorBookingsQuery : IRequest<List<VendorBookingDto>>
    {
        public required string VendorUserId { get; init; }
    }
}

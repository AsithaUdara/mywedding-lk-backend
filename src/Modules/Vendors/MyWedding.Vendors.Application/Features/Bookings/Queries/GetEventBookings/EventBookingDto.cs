using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetEventBookings
{
    public record EventBookingDto(
        Guid BookingId,
        string ServiceName,
        string VendorName,
        decimal FinalAmount,
        BookingStatus Status,
        DateTime ServiceDate
    );
}

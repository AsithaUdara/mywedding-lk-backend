using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetVendorBookings
{
    public record VendorBookingDto(
        Guid BookingId,
        string ServiceName,
        string EventName,
        string CoupleName,
        decimal FinalAmount,
        BookingStatus Status,
        DateTime ServiceDate
    );
}

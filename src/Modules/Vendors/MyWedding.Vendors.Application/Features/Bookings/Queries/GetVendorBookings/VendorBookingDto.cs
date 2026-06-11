using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetVendorBookings
{
    public record VendorBookingDto(
        Guid BookingId,
        string ServiceName,
        string EventName,
        string CoupleName,
        string? BookedByEmail,
        decimal FinalAmount,
        BookingStatus Status,
        DateTime ServiceDate,
        string? ContractFileUrl,
        DateTime? ContractSentAt,
        DateTime? ContractSignedAt,
        bool ContractUploaded
    );
}

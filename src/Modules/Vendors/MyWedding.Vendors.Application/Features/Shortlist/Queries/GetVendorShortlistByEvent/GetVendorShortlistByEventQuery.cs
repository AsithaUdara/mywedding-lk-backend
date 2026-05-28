using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Queries.GetVendorShortlistByEvent;

public class GetVendorShortlistByEventQuery : IRequest<IReadOnlyList<VendorShortlistItemDto>>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
}

public record VendorShortlistItemDto(
    Guid Id,
    Guid VendorServiceId,
    string? ServiceName,
    string? VendorBusinessName,
    string? CategoryLabel,
    string? PlannerNotes,
    string Status,
    decimal ProposedAmount,
    DateTime? ServiceDate,
    Guid? VendorBookingId,
    DateTime? SentToClientAt,
    DateTime? ClientApprovedAt);

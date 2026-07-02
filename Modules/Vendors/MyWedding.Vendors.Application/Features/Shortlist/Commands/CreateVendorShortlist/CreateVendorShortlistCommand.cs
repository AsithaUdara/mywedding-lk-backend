using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.CreateVendorShortlist;

public class CreateVendorShortlistCommand : IRequest<IReadOnlyList<Guid>>
{
    public Guid EventId { get; set; }
    public required string PlannerId { get; set; }
    public bool SendToClient { get; set; }
    public required IReadOnlyList<CreateVendorShortlistItemRequest> Items { get; set; }
}

public record CreateVendorShortlistItemRequest(
    Guid VendorServiceId,
    string? CategoryLabel,
    string? PlannerNotes,
    decimal ProposedAmount,
    DateTime? ServiceDate);

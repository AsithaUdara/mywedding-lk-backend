using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.ApproveVendorFromShortlist;

public class ApproveVendorFromShortlistCommand : IRequest<Guid>
{
    public Guid EventId { get; set; }
    public Guid ShortlistItemId { get; set; }
    public required string ClientUserId { get; set; }
    public bool Reject { get; set; }
}

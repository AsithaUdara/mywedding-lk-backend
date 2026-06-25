using MediatR;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.SendShortlistToClient;

public class SendShortlistToClientCommand : IRequest<Unit>
{
    public Guid EventId { get; set; }
    public required string PlannerId { get; set; }
    public IReadOnlyList<Guid>? ItemIds { get; set; }
}

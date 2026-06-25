using MediatR;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.MarkCommissionSettlementPaid;

public class MarkCommissionSettlementPaidCommand : IRequest<bool>
{
    public Guid SettlementId { get; set; }
}

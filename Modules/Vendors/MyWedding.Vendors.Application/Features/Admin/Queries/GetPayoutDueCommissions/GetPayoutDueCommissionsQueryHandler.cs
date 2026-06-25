using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;

public class GetPayoutDueCommissionsQueryHandler
    : IRequestHandler<GetPayoutDueCommissionsQuery, IEnumerable<PayoutDueCommissionDto>>
{
    private readonly ICommissionSettlementRepository _settlementRepository;

    public GetPayoutDueCommissionsQueryHandler(ICommissionSettlementRepository settlementRepository)
    {
        _settlementRepository = settlementRepository;
    }

    public async Task<IEnumerable<PayoutDueCommissionDto>> Handle(
        GetPayoutDueCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _settlementRepository.GetPayoutDueAsync(cancellationToken);

        return items.Select(x => new PayoutDueCommissionDto
        {
            Id = x.Id,
            BookingId = x.BookingId,
            GrossAmount = x.GrossAmount,
            CommissionAmount = x.CommissionAmount,
            VendorNetAmount = x.VendorNetAmount,
            CreatedAt = x.CreatedAt,
            ServiceId = x.ServiceId,
            EventId = x.EventId
        });
    }
}

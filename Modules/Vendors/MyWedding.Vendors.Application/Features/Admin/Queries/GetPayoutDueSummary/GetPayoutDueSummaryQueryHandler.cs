using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueSummary;

public class GetPayoutDueSummaryQueryHandler : IRequestHandler<GetPayoutDueSummaryQuery, PayoutDueSummaryDto>
{
    private readonly ICommissionSettlementRepository _settlementRepository;

    public GetPayoutDueSummaryQueryHandler(ICommissionSettlementRepository settlementRepository)
    {
        _settlementRepository = settlementRepository;
    }

    public async Task<PayoutDueSummaryDto> Handle(
        GetPayoutDueSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _settlementRepository.GetPayoutDueSummaryAsync(cancellationToken);

        return new PayoutDueSummaryDto
        {
            Count = summary.Count,
            TotalGross = summary.TotalGross,
            TotalCommission = summary.TotalCommission,
            TotalVendorNet = summary.TotalVendorNet,
        };
    }
}

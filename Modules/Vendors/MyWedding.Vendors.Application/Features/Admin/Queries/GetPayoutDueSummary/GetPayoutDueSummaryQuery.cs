using MediatR;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueSummary;

public class GetPayoutDueSummaryQuery : IRequest<PayoutDueSummaryDto>
{
}

public class PayoutDueSummaryDto
{
    public int Count { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalVendorNet { get; set; }
}

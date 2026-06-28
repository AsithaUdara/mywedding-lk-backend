using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System.Linq;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;

public class GetPayoutDueCommissionsQueryHandler
    : IRequestHandler<GetPayoutDueCommissionsQuery, PagedResult<PayoutDueCommissionDto>>
{
    private readonly ICommissionSettlementRepository _settlementRepository;

    public GetPayoutDueCommissionsQueryHandler(ICommissionSettlementRepository settlementRepository)
    {
        _settlementRepository = settlementRepository;
    }

    public async Task<PagedResult<PayoutDueCommissionDto>> Handle(
        GetPayoutDueCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _settlementRepository.GetPayoutDuePagedAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<PayoutDueCommissionDto>
        {
            Items = page.Items.Select(x => new PayoutDueCommissionDto
            {
                Id = x.Id,
                BookingId = x.BookingId,
                GrossAmount = x.GrossAmount,
                CommissionAmount = x.CommissionAmount,
                VendorNetAmount = x.VendorNetAmount,
                CreatedAt = x.CreatedAt,
                ServiceId = x.ServiceId,
                EventId = x.EventId,
            }).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }
}

using MediatR;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;

public class GetPayoutDueCommissionsQuery : IRequest<PagedResult<PayoutDueCommissionDto>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PayoutDueCommissionDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal VendorNetAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ServiceId { get; set; }
    public Guid EventId { get; set; }
}

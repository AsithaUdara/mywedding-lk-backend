using MediatR;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;

public class GetPayoutDueCommissionsQuery : IRequest<IEnumerable<PayoutDueCommissionDto>>
{
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

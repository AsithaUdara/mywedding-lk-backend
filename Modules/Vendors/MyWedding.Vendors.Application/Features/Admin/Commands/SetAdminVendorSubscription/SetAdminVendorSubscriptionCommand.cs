using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.SetAdminVendorSubscription;

public class SetAdminVendorSubscriptionCommand : IRequest<bool>
{
    public required string VendorId { get; set; }
    public SubscriptionPlanTier Tier { get; set; }
    public decimal MonthlyFee { get; set; }
}

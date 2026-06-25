using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities;

public class VendorSubscriptionCheckout
{
    public Guid Id { get; set; }
    public required string VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public SubscriptionPlanTier Tier { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class VendorSubscription
    {
        public Guid Id { get; set; }
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public SubscriptionPlanTier Tier { get; set; } = SubscriptionPlanTier.Free;
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public decimal MonthlyFee { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

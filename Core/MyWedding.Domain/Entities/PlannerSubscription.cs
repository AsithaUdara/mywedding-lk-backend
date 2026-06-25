using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class PlannerSubscription
    {
        public Guid Id { get; set; }
        public required string PlannerId { get; set; }
        public WeddingPlanner? Planner { get; set; }

        public SubscriptionPlanTier Tier { get; set; } = SubscriptionPlanTier.Free;
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public decimal MonthlyFee { get; set; }
        public int MaxConcurrentEvents { get; set; } = 1;
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

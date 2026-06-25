using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities;

public class PlannerSubscriptionCheckout
{
    public Guid Id { get; set; }
    public required string PlannerId { get; set; }
    public WeddingPlanner? Planner { get; set; }

    public SubscriptionPlanTier Tier { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

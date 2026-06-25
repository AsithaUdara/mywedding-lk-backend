namespace MyWedding.SharedKernel.Interfaces;

/// <summary>
/// Marks a MediatR request that must pass planner subscription limits before handling.
/// </summary>
public interface IPlannerSubscriptionLimitedRequest
{
    string PlannerId { get; }
}

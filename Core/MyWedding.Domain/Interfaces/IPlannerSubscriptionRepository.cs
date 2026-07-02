using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface IPlannerSubscriptionRepository
{
    Task<bool> HasActiveSubscriptionAsync(string plannerId, CancellationToken cancellationToken = default);

    Task<PlannerSubscription?> GetLatestActiveSubscriptionAsync(
        string plannerId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveClientEventsAsync(string plannerId, CancellationToken cancellationToken = default);
}

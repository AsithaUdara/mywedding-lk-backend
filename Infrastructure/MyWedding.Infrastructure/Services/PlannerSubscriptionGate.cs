using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class PlannerSubscriptionGate : IPlannerSubscriptionGate
{
    private readonly IPlannerSubscriptionRepository _subscriptionRepository;

    public PlannerSubscriptionGate(IPlannerSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task EnsureCanCreatePlannerEventAsync(string plannerId, CancellationToken cancellationToken = default)
    {
        var activeSub = await _subscriptionRepository.GetLatestActiveSubscriptionAsync(plannerId, cancellationToken);

        var tier = activeSub?.Tier ?? SubscriptionPlanTier.Free;
        var maxEvents = activeSub?.MaxConcurrentEvents ?? 1;

        var activeCount = await _subscriptionRepository.CountActiveClientEventsAsync(plannerId, cancellationToken);

        if (tier == SubscriptionPlanTier.Free && activeCount >= 1)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["subscription"] =
                [
                    "Free plan includes 1 active wedding. Upgrade to Planner Pro to manage unlimited client events."
                ],
                ["code"] = ["PLANNER_SUBSCRIPTION_LIMIT"]
            });
        }

        if (activeCount >= maxEvents)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["subscription"] =
                [
                    $"Your plan allows {maxEvents} concurrent active wedding(s). Upgrade to Planner Pro for more capacity."
                ],
                ["code"] = ["PLANNER_SUBSCRIPTION_LIMIT"]
            });
        }
    }
}

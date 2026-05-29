using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class PlannerSubscriptionGate : IPlannerSubscriptionGate
{
    private readonly ApplicationDbContext _db;

    public PlannerSubscriptionGate(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnsureCanCreatePlannerEventAsync(string plannerId, CancellationToken cancellationToken = default)
    {
        var activeSub = await _db.PlannerSubscriptions
            .AsNoTracking()
            .Where(s => s.PlannerId == plannerId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var tier = activeSub?.Tier ?? SubscriptionPlanTier.Free;
        var maxEvents = activeSub?.MaxConcurrentEvents ?? 1;

        var activeCount = await _db.PlannerClientEvents.CountAsync(
            e => e.PlannerId == plannerId && e.Status == PlannerClientEventStatus.Active,
            cancellationToken);

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

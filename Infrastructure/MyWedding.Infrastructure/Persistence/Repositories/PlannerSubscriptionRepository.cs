using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class PlannerSubscriptionRepository : IPlannerSubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public PlannerSubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasActiveSubscriptionAsync(
        string plannerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var latest = await _context.PlannerSubscriptions
            .AsNoTracking()
            .Where(s => s.PlannerId == plannerId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return true;

        if (latest.Status == SubscriptionStatus.Expired)
            return false;

        if (latest.Status == SubscriptionStatus.Active
            && latest.EndsAt is not null
            && latest.EndsAt < now)
            return false;

        return latest.Status == SubscriptionStatus.Active;
    }

    public Task<PlannerSubscription?> GetLatestActiveSubscriptionAsync(
        string plannerId,
        CancellationToken cancellationToken = default) =>
        _context.PlannerSubscriptions
            .AsNoTracking()
            .Where(s => s.PlannerId == plannerId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountActiveClientEventsAsync(
        string plannerId,
        CancellationToken cancellationToken = default) =>
        _context.PlannerClientEvents.CountAsync(
            e => e.PlannerId == plannerId && e.Status == PlannerClientEventStatus.Active,
            cancellationToken);
}

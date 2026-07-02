using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class WeddingPlannerProfileReader : IWeddingPlannerProfileReader
{
    private readonly ApplicationDbContext _db;

    public WeddingPlannerProfileReader(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<WeddingPlannerProfileSnapshot?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var planner = await _db.WeddingPlanners
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (planner is null)
            return null;

        var displayName = planner.User is null
            ? planner.BusinessName
            : $"{planner.User.FirstName} {planner.User.LastName}".Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = planner.BusinessName;

        return new WeddingPlannerProfileSnapshot(
            displayName,
            planner.BusinessName,
            planner.AgencyLogoUrl);
    }

    public async Task<EventPlannerBrandingSnapshot?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.PlannerClientEvents
            .AsNoTracking()
            .Include(e => e.Planner!)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (link?.Planner is null)
            return null;

        var planner = link.Planner;
        var displayName = planner.User is null
            ? planner.BusinessName
            : $"{planner.User.FirstName} {planner.User.LastName}".Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = planner.BusinessName;

        var activeSub = await _db.PlannerSubscriptions
            .AsNoTracking()
            .Where(s => s.PlannerId == link.PlannerId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isPro = activeSub?.Tier == SubscriptionPlanTier.PlannerPro;
        var logoUrl = isPro && !string.IsNullOrWhiteSpace(planner.AgencyLogoUrl)
            ? planner.AgencyLogoUrl
            : null;

        return new EventPlannerBrandingSnapshot(
            planner.BusinessName,
            displayName,
            logoUrl,
            logoUrl is not null);
    }
}

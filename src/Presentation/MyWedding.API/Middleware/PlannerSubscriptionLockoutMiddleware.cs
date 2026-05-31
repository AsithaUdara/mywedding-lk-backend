using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.API.Middleware;

/// <summary>
/// Blocks CRM planner API calls when the planner's subscription is expired (402 Payment Required).
/// </summary>
public class PlannerSubscriptionLockoutMiddleware
{
    private readonly RequestDelegate _next;

    public PlannerSubscriptionLockoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentPlannerAccessor plannerAccessor,
        IServiceScopeFactory scopeFactory)
    {
        if (plannerAccessor.IsPlanner &&
            IsProtectedPlannerPath(context.Request.Path) &&
            !IsExemptPath(context.Request.Path, context.Request.Method))
        {
            var plannerId = plannerAccessor.PlannerId;
            if (!string.IsNullOrWhiteSpace(plannerId))
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (!await HasActivePlannerSubscriptionAsync(db, plannerId, context.RequestAborted))
                    throw new PaymentRequiredException();
            }
        }

        await _next(context);
    }

    private static bool IsProtectedPlannerPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/api/planner", StringComparison.OrdinalIgnoreCase))
            return true;

        return value.Contains("/api/planner/events/", StringComparison.OrdinalIgnoreCase)
               && value.Contains("/vendor-shortlist", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExemptPath(PathString path, string method)
    {
        var value = path.Value ?? string.Empty;

        if (value.Equals("/api/planner/signup", StringComparison.OrdinalIgnoreCase)
            && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("/api/planner/overview", StringComparison.OrdinalIgnoreCase)
            && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("/api/planner/subscription", StringComparison.OrdinalIgnoreCase)
            && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static async Task<bool> HasActivePlannerSubscriptionAsync(
        ApplicationDbContext db,
        string plannerId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var latest = await db.PlannerSubscriptions
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
}

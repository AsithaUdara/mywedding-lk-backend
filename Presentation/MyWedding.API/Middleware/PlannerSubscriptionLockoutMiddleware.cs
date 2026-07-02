using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;

#pragma warning disable CS1591 // HTTP middleware — not part of OpenAPI

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
        IPlannerSubscriptionRepository subscriptionRepository)
    {
        if (plannerAccessor.IsPlanner &&
            IsProtectedPlannerPath(context.Request.Path) &&
            !IsExemptPath(context.Request.Path, context.Request.Method))
        {
            var plannerId = plannerAccessor.PlannerId;
            if (!string.IsNullOrWhiteSpace(plannerId)
                && !await subscriptionRepository.HasActiveSubscriptionAsync(plannerId, context.RequestAborted))
            {
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
}

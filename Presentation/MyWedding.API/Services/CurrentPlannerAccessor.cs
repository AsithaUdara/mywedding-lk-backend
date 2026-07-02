using System.Security.Claims;
using MyWedding.Domain.Interfaces;

#pragma warning disable CS1591 // Internal service — not part of OpenAPI

namespace MyWedding.API.Services;

public class CurrentPlannerAccessor : ICurrentPlannerAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentPlannerAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? PlannerId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsPlanner =>
        string.Equals(
            _httpContextAccessor.HttpContext?.User.FindFirstValue("role"),
            "planner",
            StringComparison.OrdinalIgnoreCase);
}

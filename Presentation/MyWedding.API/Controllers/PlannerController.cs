using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Enums;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlannerController : ControllerBase
{
    private readonly IPlannerWorkspaceService _plannerWorkspace;

    public PlannerController(IPlannerWorkspaceService plannerWorkspace)
    {
        _plannerWorkspace = plannerWorkspace;
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] PlannerSignupRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.SignupAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetDashboardAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetOverviewAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetClientsAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetPlannerEvents([FromQuery] PlannerClientEventStatus? status, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetPlannerEventsAsync(
            GetCurrentUserId(),
            status?.ToString(),
            cancellationToken);
        return MapResult(result);
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetPlannerBookings(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetPlannerBookingsAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    [HttpPatch("events/{eventId:guid}/stage")]
    public async Task<IActionResult> UpdateEventStage(Guid eventId, [FromBody] UpdateEventLifecycleStageRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateEventStageAsync(eventId, request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreatePlannerEvent([FromBody] CreatePlannerEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.CreatePlannerEventAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("events/{eventId:guid}/assign-client")]
    public async Task<IActionResult> AssignClient(Guid eventId, [FromBody] AssignPlannerClientRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.AssignClientAsync(GetCurrentUserId(), eventId, request, cancellationToken);
        return MapResult(result);
    }

    [HttpGet("events/{eventId:guid}/access-check")]
    public async Task<IActionResult> CheckPlannerEventAccess(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.CheckPlannerEventAccessAsync(GetCurrentUserId(), eventId, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdatePlannerSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateSubscriptionAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    [HttpGet("billing-profile")]
    public async Task<IActionResult> GetBillingProfile(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetBillingProfileAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    [HttpPut("billing-profile")]
    public async Task<IActionResult> SaveBillingProfile(
        [FromBody] PlannerBillingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.SaveBillingProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePlannerProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    [HttpPut("profile/agency-logo")]
    public async Task<IActionResult> UpdateAgencyLogo(
        [FromBody] UpdatePlannerAgencyLogoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateAgencyLogoAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    private IActionResult MapResult(PlannerWorkflowResult result) => result.StatusCode switch
    {
        200 => result.Body is null ? Ok() : Ok(result.Body),
        400 => BadRequest(result.Body),
        401 => result.Body is null ? Unauthorized() : Unauthorized(result.Body),
        403 => result.Body is null ? Forbid() : StatusCode(StatusCodes.Status403Forbidden, result.Body),
        404 => result.Body is null ? NotFound() : NotFound(result.Body),
        _ => StatusCode(result.StatusCode, result.Body)
    };
}

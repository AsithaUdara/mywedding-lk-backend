using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Enums;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Wedding planner workspace: dashboard, clients, events, bookings, profile, and billing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlannerController : ControllerBase
{
    private readonly IPlannerWorkspaceService _plannerWorkspace;
        /// <summary>
        /// Initializes a new instance of the <see cref="PlannerController"/> class.
        /// </summary>
    public PlannerController(IPlannerWorkspaceService plannerWorkspace)
    {
        _plannerWorkspace = plannerWorkspace;
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Completes planner onboarding and creates the planner profile.
    /// </summary>
    /// <param name="request">Agency name, contact details, and service areas.</param>
    /// <response code="200">Planner profile created.</response>
    /// <response code="400">Invalid signup data.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("signup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Signup([FromBody] PlannerSignupRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.SignupAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Returns the planner dashboard summary (active events, tasks, revenue).
    /// </summary>
    /// <response code="200">Dashboard data returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetDashboardAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Returns a high-level overview of the planner's portfolio and pipeline.
    /// </summary>
    /// <response code="200">Overview data returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetOverviewAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Lists all clients managed by the authenticated planner.
    /// </summary>
    /// <response code="200">Client list returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("clients")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetClientsAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Lists events managed by the planner, optionally filtered by client lifecycle stage.
    /// </summary>
    /// <param name="status">Optional filter: Inquiry, Planning, Confirmed, Completed, etc.</param>
    /// <response code="200">Events returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPlannerEvents([FromQuery] PlannerClientEventStatus? status, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetPlannerEventsAsync(
            GetCurrentUserId(),
            status?.ToString(),
            cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Lists all vendor bookings across the planner's managed events.
    /// </summary>
    /// <response code="200">Bookings returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPlannerBookings(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetPlannerBookingsAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Updates the lifecycle stage of a planner-managed event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">The new lifecycle stage.</param>
    /// <response code="200">Stage updated.</response>
    /// <response code="403">Caller cannot manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPatch("events/{eventId:guid}/stage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEventStage(Guid eventId, [FromBody] UpdateEventLifecycleStageRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateEventStageAsync(eventId, request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Creates a new wedding event under the planner's management.
    /// </summary>
    /// <param name="request">Event name, date, and client assignment details.</param>
    /// <response code="200">Event created.</response>
    /// <response code="400">Invalid event data.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePlannerEvent([FromBody] CreatePlannerEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.CreatePlannerEventAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Assigns a client user to a planner-managed event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">Client user ID and role details.</param>
    /// <response code="200">Client assigned.</response>
    /// <response code="403">Caller cannot manage this event.</response>
    [HttpPost("events/{eventId:guid}/assign-client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignClient(Guid eventId, [FromBody] AssignPlannerClientRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.AssignClientAsync(GetCurrentUserId(), eventId, request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Verifies whether the authenticated planner has access to a specific event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <response code="200">Access check result returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("events/{eventId:guid}/access-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckPlannerEventAccess(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.CheckPlannerEventAccessAsync(GetCurrentUserId(), eventId, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Updates the planner's subscription tier (self-service).
    /// </summary>
    /// <param name="request">Subscription tier and monthly fee.</param>
    /// <response code="200">Subscription updated.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdatePlannerSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateSubscriptionAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Returns the planner's billing profile for invoices and payments.
    /// </summary>
    /// <response code="200">Billing profile returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("billing-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBillingProfile(CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.GetBillingProfileAsync(GetCurrentUserId(), cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Saves or updates the planner's billing profile.
    /// </summary>
    /// <param name="request">Company name, address, and tax details.</param>
    /// <response code="200">Billing profile saved.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("billing-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveBillingProfile(
        [FromBody] PlannerBillingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.SaveBillingProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Updates the planner's public profile and agency details.
    /// </summary>
    /// <param name="request">Agency name, bio, contact, and service areas.</param>
    /// <response code="200">Profile updated.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePlannerProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerWorkspace.UpdateProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Updates the planner agency logo URL.
    /// </summary>
    /// <param name="request">Logo image URL.</param>
    /// <response code="200">Logo updated.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("profile/agency-logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

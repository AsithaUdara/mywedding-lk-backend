using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;





using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Manages the organizers (team members) of a wedding event, including invitations and role management.
/// </summary>
[ApiController]
[Route("api/events/{eventId}/organizers")]
[Authorize]
public class EventOrganizersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEventOrganizerRepository _organizerRepository;

    public EventOrganizersController(IMediator mediator, IEventOrganizerRepository organizerRepository)
    {
        _mediator = mediator;
        _organizerRepository = organizerRepository;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Retrieves all organizers for a specific event. Requires the caller to be a member.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <returns>A list of organizers with their roles and permissions.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrganizersForEvent(Guid eventId)
    {
        var currentUserId = GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var inviter = await _organizerRepository.GetOrganizerAsync(eventId, currentUserId);
        if (inviter == null)
            return Forbid();

        var query = new GetOrganizersByEventIdQuery { EventId = eventId };
        var organizers = await _mediator.Send(query);

        return Ok(organizers);
    }

    /// <summary>
    /// Sends an invitation to a user to join the event as an organizer.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <param name="request">The invitation details including email, role, and permission level.</param>
    /// <returns>200 OK on success, or appropriate error status.</returns>
    [HttpPost]
    public async Task<IActionResult> InviteUserToEvent(Guid eventId, [FromBody] InviteUserRequest request)
    {
        var inviterUserId = GetUserId();
        if (string.IsNullOrEmpty(inviterUserId))
            return Unauthorized();

        var command = new InviteUserToEventCommand
        {
            EventId = eventId,
            InviteeEmail = request.Email,
            Role = request.Role,
            PermissionLevel = request.PermissionLevel,
            InviterUserId = inviterUserId
        };

        // Domain exceptions are handled by the global ExceptionMiddleware
        await _mediator.Send(command);

        return Ok(new { message = "Invitation sent successfully." });
    }

    /// <summary>
    /// Updates the role and permission level of an existing organizer.
    /// Only the event owner or an organizer with appropriate permissions can perform this action.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <param name="userId">The ID of the organizer to update.</param>
    /// <param name="request">The new role and permission level.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateOrganizerRole(Guid eventId, string userId, [FromBody] UpdateOrganizerRequest request)
    {
        var currentUserId = GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var command = new UpdateOrganizerCommand
        {
            EventId = eventId,
            TargetUserId = userId,
            RequestingUserId = currentUserId,
            Role = request.Role,
            PermissionLevel = request.PermissionLevel
        };

        // Domain exceptions are handled by the global ExceptionMiddleware
        await _mediator.Send(command);

        return Ok(new { message = "Organizer updated successfully." });
    }
}

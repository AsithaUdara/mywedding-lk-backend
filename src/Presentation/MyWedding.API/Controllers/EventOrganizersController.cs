// File: src/Presentation/MyWedding.API/Controllers/EventOrganizersController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.EventOrganizers.Commands.InviteUserToEvent;
using MyWedding.Application.Features.EventOrganizers.Queries.GetOrganizersByEventId; // <-- ADD THIS
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces; // <-- ADD THIS
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/events/{eventId}/organizers")]
[Authorize]
public class EventOrganizersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEventOrganizerRepository _organizerRepository; // <-- ADD THIS

    public EventOrganizersController(IMediator mediator, IEventOrganizerRepository organizerRepository)
    {
        _mediator = mediator;
        _organizerRepository = organizerRepository;
    }

    // --- NEW GET ENDPOINT ---
    [HttpGet]
    public async Task<IActionResult> GetOrganizersForEvent(Guid eventId)
    {
        // Security Check: First, ensure the current user is a member of this event
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var inviter = await _organizerRepository.GetOrganizerAsync(eventId, currentUserId);
        if (inviter == null)
        {
            return Forbid(); // If you're not a member, you can't see the list of members
        }

        var query = new GetOrganizersByEventIdQuery { EventId = eventId };
        var organizers = await _mediator.Send(query);

        return Ok(organizers);
    }
    
    [HttpPost]
    public async Task<IActionResult> InviteUserToEvent(Guid eventId, [FromBody] InviteUserRequest request)
    {
        var inviterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(inviterUserId))
        {
            return Unauthorized();
        }

        var command = new InviteUserToEventCommand
        {
            EventId = eventId,
            InviteeEmail = request.Email,
            Role = request.Role,
            PermissionLevel = request.PermissionLevel,
            InviterUserId = inviterUserId
        };

        try
        {
            await _mediator.Send(command);
        }
        catch (MyWedding.Application.Common.Exceptions.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MyWedding.Application.Common.Exceptions.ForbiddenAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        return Ok(new { message = "Invitation successful." });
    }

    // --- NEW PUT ENDPOINT ---
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateOrganizerRole(Guid eventId, string userId, [FromBody] UpdateOrganizerRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var command = new MyWedding.Application.Features.EventOrganizers.Commands.UpdateOrganizer.UpdateOrganizerCommand
        {
            EventId = eventId,
            TargetUserId = userId,
            RequestingUserId = currentUserId,
            Role = request.Role,
            PermissionLevel = request.PermissionLevel
        };

        try
        {
            await _mediator.Send(command);
        }
        catch (MyWedding.Application.Common.Exceptions.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MyWedding.Application.Common.Exceptions.ForbiddenAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }

        return Ok(new { message = "Organizer updated successfully." });
    }
}

public record InviteUserRequest(string Email, OrganizerRole Role, PermissionLevel PermissionLevel);
public record UpdateOrganizerRequest(OrganizerRole Role, PermissionLevel PermissionLevel);

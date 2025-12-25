// File: src/Presentation/MyWedding.API/Controllers/EventOrganizersController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.EventOrganizers.Commands.InviteUserToEvent;
using MyWedding.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/events/{eventId}/organizers")]
[Authorize]
public class EventOrganizersController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventOrganizersController(IMediator mediator)
    {
        _mediator = mediator;
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

        return Ok(new { message = "Invitation sent successfully." });
    }
}

public record InviteUserRequest(string Email, OrganizerRole Role, PermissionLevel PermissionLevel);

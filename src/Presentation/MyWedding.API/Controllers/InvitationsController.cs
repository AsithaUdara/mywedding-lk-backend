using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Invitations.Commands.AcceptInvitation;
using MyWedding.Application.Features.Invitations.Commands.InviteMember;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvitationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InvitationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // POST /api/invitations/invite
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember([FromBody] InviteRequest request)
        {
            try 
            {
                var command = new InviteMemberCommand
                {
                    EventId = request.EventId,
                    Email = request.Email,
                    InvitedById = GetUserId(),
                    Role = request.Role ?? MyWedding.Domain.Enums.OrganizerRole.Friend,
                    PermissionLevel = request.PermissionLevel ?? MyWedding.Domain.Enums.PermissionLevel.Editor
                };

                var invitationId = await _mediator.Send(command);
                return Ok(new { InvitationId = invitationId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // POST /api/invitations/accept
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptRequest request)
        {
            try 
            {
                var command = new AcceptInvitationCommand
                {
                    Token = request.Token,
                    UserId = GetUserId()
                };

                var result = await _mediator.Send(command);
                
                if (!result)
                {
                    return BadRequest("Invalid or expired invitation token.");
                }

                return Ok(new { Message = "Invitation accepted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }

    public record InviteRequest(Guid EventId, string Email, MyWedding.Domain.Enums.OrganizerRole? Role, MyWedding.Domain.Enums.PermissionLevel? PermissionLevel);
    public record AcceptRequest(string Token);
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Event team invitations: invite members by email and accept invitation tokens.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvitationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationsController"/> class.
        /// </summary>
        public InvitationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        /// <summary>
        /// Sends an invitation for a user to join an event as an organizer.
        /// </summary>
        /// <param name="request">Event ID, invitee email, role, and permission level.</param>
        /// <returns>Invitation ID, accept URL, and email delivery status.</returns>
        /// <response code="200">Invitation created (email may or may not have been sent).</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost]
        [HttpPost("invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> InviteMember([FromBody] InviteRequest request)
        {
            var command = new InviteMemberCommand
            {
                EventId = request.EventId,
                Email = request.Email,
                InvitedById = GetUserId(),
                Role = request.Role ?? MyWedding.Domain.Enums.OrganizerRole.Friend,
                PermissionLevel = request.PermissionLevel ?? MyWedding.Domain.Enums.PermissionLevel.Editor
            };

            var result = await _mediator.Send(command);
            return Ok(new
            {
                invitationId = result.InvitationId,
                emailSent = result.EmailSent,
                acceptUrl = result.AcceptUrl,
                emailError = result.EmailError,
                message = result.EmailSent
                    ? "Invitation sent successfully."
                    : "Invitation created. Email delivery failed â€” share the accept link with your client."
            });
        }

        /// <summary>
        /// Accepts an event invitation using the token from the invitation email.
        /// </summary>
        /// <param name="request">The invitation acceptance token.</param>
        /// <returns>Confirmation that the invitation was accepted.</returns>
        /// <response code="200">Invitation accepted.</response>
        /// <response code="400">Token is invalid or expired.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("accept")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptRequest request)
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
    }

    /// <summary>Payload for sending an event team invitation.</summary>
    public record InviteRequest(Guid EventId, string Email, MyWedding.Domain.Enums.OrganizerRole? Role, MyWedding.Domain.Enums.PermissionLevel? PermissionLevel);

    /// <summary>Payload for accepting an invitation token.</summary>
    public record AcceptRequest(string Token);
}

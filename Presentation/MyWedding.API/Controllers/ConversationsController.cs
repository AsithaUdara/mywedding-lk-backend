// File: src/Presentation/MyWedding.API/Controllers/ConversationsController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Event conversations and messaging between organizers, planners, and vendors.
    /// </summary>
    [ApiController]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationsController"/> class.
        /// </summary>
        public ConversationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lists all conversations for a wedding event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <response code="200">Conversations returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("api/events/{eventId:guid}/conversations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetConversationsForEvent(Guid eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetConversationsByEventIdQuery { EventId = eventId, UserId = userId };
            var conversations = await _mediator.Send(query);
            return Ok(conversations);
        }

        /// <summary>
        /// Retrieves all messages in a conversation thread.
        /// </summary>
        /// <param name="conversationId">The conversation identifier.</param>
        /// <response code="200">Messages returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("api/conversations/{conversationId:guid}/messages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMessagesForConversation(Guid conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetMessagesByConversationIdQuery { ConversationId = conversationId, UserId = userId };
            var messages = await _mediator.Send(query);
            return Ok(messages);
        }

        /// <summary>
        /// Posts a new message to a conversation (optionally with task, expense, or vendor attachments).
        /// </summary>
        /// <param name="conversationId">The conversation identifier.</param>
        /// <param name="request">Message content and optional attachment references.</param>
        /// <response code="201">Message created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("api/conversations/{conversationId:guid}/messages")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PostMessage(Guid conversationId, [FromBody] PostMessageRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new PostMessageCommand
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = request.Content,
                AttachedEventTaskId = request.AttachedEventTaskId,
                AttachedExpenseId = request.AttachedExpenseId,
                AttachedVendorServiceId = request.AttachedVendorServiceId
            };

            var messageId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetMessagesForConversation), new { conversationId = conversationId }, new { MessageId = messageId });
        }
    }

    /// <summary>Payload for posting a conversation message.</summary>
    public record PostMessageRequest(
        string Content,
        Guid? AttachedVendorServiceId,
        Guid? AttachedEventTaskId,
        Guid? AttachedExpenseId
    );
}

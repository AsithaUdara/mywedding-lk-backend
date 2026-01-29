// File: src/Presentation/MyWedding.API/Controllers/ConversationsController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Conversations.Queries.GetConversationsByEventId;
using MyWedding.Application.Features.Messages.Commands.PostMessage;
using MyWedding.Application.Features.Messages.Queries.GetMessagesByConversationId;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Authorize] // All chat features require a user to be logged in
    public class ConversationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ConversationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/events/{eventId}/conversations
        [HttpGet("api/events/{eventId:guid}/conversations")]
        public async Task<IActionResult> GetConversationsForEvent(Guid eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetConversationsByEventIdQuery { EventId = eventId, UserId = userId };
            var conversations = await _mediator.Send(query);
            return Ok(conversations);
        }

        // GET /api/conversations/{conversationId}/messages
        [HttpGet("api/conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> GetMessagesForConversation(Guid conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetMessagesByConversationIdQuery { ConversationId = conversationId, UserId = userId };
            var messages = await _mediator.Send(query);
            return Ok(messages);
        }

        // POST /api/conversations/{conversationId}/messages
        [HttpPost("api/conversations/{conversationId:guid}/messages")]
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

    // DTO for the PostMessage request body
    public record PostMessageRequest(
        string Content,
        Guid? AttachedVendorServiceId,
        Guid? AttachedEventTaskId,
        Guid? AttachedExpenseId
    );
}

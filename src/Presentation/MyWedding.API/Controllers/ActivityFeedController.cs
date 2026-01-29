// File: src/Presentation/MyWedding.API/Controllers/ActivityFeedController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.ActivityFeed.Commands.PostComment;
using MyWedding.Application.Features.ActivityFeed.Queries.GetActivityFeedByEventId;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:guid}/activity")]
    [Authorize]
    public class ActivityFeedController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActivityFeedController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/events/{eventId}/activity
        [HttpGet]
        public async Task<IActionResult> GetActivityFeed(Guid eventId)
        {
            // TODO: Add a security check to ensure the user is a member of this event
            var query = new GetActivityFeedByEventIdQuery { EventId = eventId };
            var feedItems = await _mediator.Send(query);
            return Ok(feedItems);
        }

        // POST /api/events/{eventId}/activity/comments
        [HttpPost("comments")]
        public async Task<IActionResult> PostComment(Guid eventId, [FromBody] PostCommentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // TODO: Add a security check to ensure the user is a member of this event
            var command = new PostCommentCommand
            {
                EventId = eventId,
                UserId = userId,
                Content = request.Content
            };

            var commentId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetActivityFeed), new { eventId = eventId }, new { CommentId = commentId });
        }
    }

    // DTO for the request body
    public record PostCommentRequest(string Content);
}

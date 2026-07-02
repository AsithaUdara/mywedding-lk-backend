using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Event activity feed for organizers and managing planners.
    /// </summary>
    [ApiController]
    [Authorize]
    public class ActivityFeedController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityFeedController"/> class.
        /// </summary>
        public ActivityFeedController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Returns event activity for organizers and managing planners (backed by audit log).
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <response code="200">Activity feed returned.</response>
        [HttpGet("api/events/{eventId:guid}/activity")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivityFeed(Guid eventId)
        {
            var query = new GetAuditLogByEventIdQuery
            {
                EventId = eventId,
                UserId = GetUserId(),
            };

            var auditItems = await _mediator.Send(query);

            var feed = auditItems.Select(item => new
            {
                id = item.Id,
                itemType = item.ActionType == "UserComment" ? "UserComment" : "SystemLog",
                content = item.Content,
                createdAt = item.TimestampUtc,
                userId = item.ActorId,
                userFirstName = item.ActorFirstName,
                userLastName = item.ActorLastName,
                actorDisplayName = item.ActorDisplayName,
            });

            return Ok(feed);
        }
    }
}

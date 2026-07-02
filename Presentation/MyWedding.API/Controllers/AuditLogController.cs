using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Event audit log for planners and admins (compliance and activity tracking).
    /// </summary>
    [ApiController]
    [Route("api/events/{eventId:guid}/audit-log")]
    [Authorize(Roles = "planner,admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogController"/> class.
        /// </summary>
        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Returns the full audit log for an event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <response code="200">Audit log entries returned.</response>
        /// <response code="403">Caller is not a planner or admin.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAuditLog(Guid eventId)
        {
            var query = new GetAuditLogByEventIdQuery { EventId = eventId, UserId = GetUserId() };
            var auditItems = await _mediator.Send(query);
            return Ok(auditItems);
        }
    }
}

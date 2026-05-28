using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:guid}/audit-log")]
    [Authorize(Roles = "planner,admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLog(Guid eventId)
        {
            var query = new GetAuditLogByEventIdQuery { EventId = eventId };
            var auditItems = await _mediator.Send(query);
            return Ok(auditItems);
        }
    }
}

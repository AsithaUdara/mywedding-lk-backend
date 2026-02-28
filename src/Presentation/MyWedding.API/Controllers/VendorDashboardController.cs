// File: src/Presentation/MyWedding.API/Controllers/VendorDashboardController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Vendors.Commands.AddService;
using MyWedding.Application.Features.Vendors.Commands.UpdateService;
using MyWedding.Application.Features.Vendors.Commands.DeleteService;
using MyWedding.Application.Features.Vendors.Queries.GetVendorServices;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/vendor/dashboard")]
    [Authorize] // Requires authentication
    public class VendorDashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VendorDashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // GET /api/vendor/dashboard/services
        [HttpGet("services")]
        public async Task<IActionResult> GetMyServices()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetVendorServicesQuery { VendorId = userId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // POST /api/vendor/dashboard/services
        [HttpPost("services")]
        public async Task<IActionResult> AddService([FromBody] AddServiceCommand command)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            command.VendorId = userId; // Ensure vendor can only add to their own account
            var result = await _mediator.Send(command);
            return Ok(new { id = result });
        }

        // PUT /api/vendor/dashboard/services/{id}
        [HttpPut("services/{id}")]
        public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return result ? Ok() : NotFound();
        }

        // DELETE /api/vendor/dashboard/services/{id}
        [HttpDelete("services/{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var command = new DeleteServiceCommand { Id = id };
            var result = await _mediator.Send(command);
            return result ? Ok() : NotFound();
        }
    }
}

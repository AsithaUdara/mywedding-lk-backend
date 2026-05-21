using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPendingVendors;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformStats;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private bool IsAdmin() =>
            User.FindFirst("role")?.Value == "admin" ||
            User.FindFirst(ClaimTypes.Role)?.Value == "admin";

        // ──────────────────────────────────────────────────────
        // GET /api/admin/vendors/pending
        // ──────────────────────────────────────────────────────
        /// <summary>Lists all vendors whose VerificationStatus is Pending.</summary>
        [HttpGet("vendors/pending")]
        public async Task<IActionResult> GetPendingVendors()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPendingVendorsQuery());
            return Ok(result);
        }

        // ──────────────────────────────────────────────────────
        // PATCH /api/admin/vendors/{id}/verify
        // ──────────────────────────────────────────────────────
        /// <summary>Approves a vendor (sets VerificationStatus = Verified).</summary>
        [HttpPatch("vendors/{id}/verify")]
        public async Task<IActionResult> VerifyVendor(string id)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new VerifyVendorCommand { VendorId = id, IsApproved = true });
            return Ok(new { success = result });
        }

        // ──────────────────────────────────────────────────────
        // PATCH /api/admin/vendors/{id}/reject
        // ──────────────────────────────────────────────────────
        /// <summary>Rejects a vendor (sets VerificationStatus = Rejected).</summary>
        [HttpPatch("vendors/{id}/reject")]
        public async Task<IActionResult> RejectVendor(string id)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new VerifyVendorCommand { VendorId = id, IsApproved = false });
            return Ok(new { success = result });
        }

        // ──────────────────────────────────────────────────────
        // GET /api/admin/stats
        // ──────────────────────────────────────────────────────
        /// <summary>Returns platform-wide statistics: total users, vendors, events, and bookings.</summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPlatformStatsQuery());
            return Ok(result);
        }
    }
}

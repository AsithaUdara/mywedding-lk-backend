using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Enums;
using MyWedding.Vendors.Application.Features.Admin.Commands.MarkCommissionSettlementPaid;
using MyWedding.Vendors.Application.Features.Admin.Commands.SetAdminVendorSubscription;
using MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendorSummary;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPendingVendors;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformAnalytics;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformStats;
using System.Security.Claims;

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

        /// <summary>Portfolio-wide vendor counts for the admin directory.</summary>
        [HttpGet("vendors/summary")]
        public async Task<IActionResult> GetAdminVendorSummary(CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var summary = await _mediator.Send(new GetAdminVendorSummaryQuery(), cancellationToken);
            return Ok(summary);
        }

        /// <summary>Lists vendors with optional status filter, search, and pagination.</summary>
        [HttpGet("vendors")]
        public async Task<IActionResult> GetAdminVendors(
            [FromQuery] VerificationStatus? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (!IsAdmin()) return Forbid();

            var vendors = await _mediator.Send(new GetAdminVendorsQuery
            {
                Status = status,
                Search = search,
                Page = page,
                PageSize = pageSize,
            }, cancellationToken);
            return Ok(vendors);
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

        [HttpGet("platform-analytics")]
        public async Task<IActionResult> GetPlatformAnalytics()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPlatformAnalyticsQuery());
            return Ok(result);
        }

        [HttpGet("commissions/payout-due")]
        public async Task<IActionResult> GetPayoutDue(CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var items = await _mediator.Send(new GetPayoutDueCommissionsQuery(), cancellationToken);
            return Ok(items);
        }

        [HttpPatch("commissions/{settlementId:guid}/mark-settled")]
        public async Task<IActionResult> MarkSettlementAsPaid(Guid settlementId, CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            await _mediator.Send(new MarkCommissionSettlementPaidCommand { SettlementId = settlementId }, cancellationToken);
            return Ok(new { message = "Settlement marked as paid." });
        }

        [HttpPatch("vendors/{vendorId}/subscription")]
        public async Task<IActionResult> SetVendorSubscription(string vendorId, [FromBody] SetVendorSubscriptionRequest request, CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            await _mediator.Send(new SetAdminVendorSubscriptionCommand
            {
                VendorId = vendorId,
                Tier = request.Tier,
                MonthlyFee = request.MonthlyFee
            }, cancellationToken);

            return Ok(new { message = "Vendor subscription updated." });
        }
    }
}

public record SetVendorSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

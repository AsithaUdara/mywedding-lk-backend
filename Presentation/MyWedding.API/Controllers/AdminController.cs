using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Enums;
using MyWedding.Vendors.Application.Features.Admin.Commands.MarkCommissionSettlementPaid;
using MyWedding.Vendors.Application.Features.Admin.Commands.SetAdminVendorSubscription;
using MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendorSummary;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors;
using MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueSummary;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPendingVendors;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformAnalytics;
using MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformStats;
using System.Security.Claims;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Platform administration: vendor verification, analytics, commissions, and audit logs.
    /// All endpoints require the admin role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController"/> class.
        /// </summary>
        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private bool IsAdmin() =>
            User.FindFirst("role")?.Value == "admin" ||
            User.FindFirst(ClaimTypes.Role)?.Value == "admin";

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // GET /api/admin/vendors/pending
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>Lists all vendors whose VerificationStatus is Pending.</summary>
        /// <response code="200">Pending vendors returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("vendors/pending")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPendingVendors()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPendingVendorsQuery());
            return Ok(result);
        }

        /// <summary>Portfolio-wide vendor counts for the admin directory.</summary>
        /// <response code="200">Summary returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("vendors/summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAdminVendorSummary(CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var summary = await _mediator.Send(new GetAdminVendorSummaryQuery(), cancellationToken);
            return Ok(summary);
        }

        /// <summary>Lists vendors with optional status filter, search, and pagination.</summary>
        /// <param name="status">Filter by verification status.</param>
        /// <param name="search">Search by business name or email.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Items per page.</param>
        /// <response code="200">Vendors returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("vendors")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // PATCH /api/admin/vendors/{id}/verify
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>Approves a vendor (sets VerificationStatus = Verified).</summary>
        /// <param name="id">The vendor identifier.</param>
        /// <response code="200">Vendor verified.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpPatch("vendors/{id}/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> VerifyVendor(string id)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new VerifyVendorCommand { VendorId = id, IsApproved = true });
            return Ok(new { success = result });
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // PATCH /api/admin/vendors/{id}/reject
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>Rejects a vendor (sets VerificationStatus = Rejected).</summary>
        /// <param name="id">The vendor identifier.</param>
        /// <response code="200">Vendor rejected.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpPatch("vendors/{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RejectVendor(string id)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new VerifyVendorCommand { VendorId = id, IsApproved = false });
            return Ok(new { success = result });
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // GET /api/admin/stats
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>Returns platform-wide statistics: total users, vendors, events, and bookings.</summary>
        /// <response code="200">Stats returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStats()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPlatformStatsQuery());
            return Ok(result);
        }

        /// <summary>Returns detailed platform analytics (growth, revenue, engagement trends).</summary>
        /// <response code="200">Analytics returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("platform-analytics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPlatformAnalytics()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _mediator.Send(new GetPlatformAnalyticsQuery());
            return Ok(result);
        }

        /// <summary>Returns a summary of commission payouts currently due to vendors.</summary>
        /// <response code="200">Payout summary returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("commissions/payout-due/summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPayoutDueSummary(CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var summary = await _mediator.Send(new GetPayoutDueSummaryQuery(), cancellationToken);
            return Ok(summary);
        }

        /// <summary>Lists individual commission settlements awaiting payout.</summary>
        /// <param name="search">Optional search by vendor name.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Items per page.</param>
        /// <response code="200">Settlements returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("commissions/payout-due")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPayoutDue(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (!IsAdmin()) return Forbid();

            var items = await _mediator.Send(new GetPayoutDueCommissionsQuery
            {
                Search = search,
                Page = page,
                PageSize = pageSize,
            }, cancellationToken);
            return Ok(items);
        }

        /// <summary>Marks a commission settlement as paid to the vendor.</summary>
        /// <param name="settlementId">The settlement identifier.</param>
        /// <response code="200">Settlement marked as paid.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpPatch("commissions/{settlementId:guid}/mark-settled")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkSettlementAsPaid(Guid settlementId, CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            await _mediator.Send(new MarkCommissionSettlementPaidCommand { SettlementId = settlementId }, cancellationToken);
            return Ok(new { message = "Settlement marked as paid." });
        }

        /// <summary>Sets or updates a vendor's subscription tier (admin override).</summary>
        /// <param name="vendorId">The vendor identifier.</param>
        /// <param name="request">Subscription tier and monthly fee.</param>
        /// <response code="200">Subscription updated.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpPatch("vendors/{vendorId}/subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>Returns a summary of audit log activity for an event (admin view).</summary>
        /// <param name="eventId">The event identifier.</param>
        /// <response code="200">Audit summary returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId:guid}/audit-log/summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdminEventAuditSummary(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var summary = await _mediator.Send(
                new GetAdminEventAuditSummaryQuery { EventId = eventId },
                cancellationToken);

            return summary is null ? NotFound() : Ok(summary);
        }

        /// <summary>Returns paginated audit log entries for an event (admin view).</summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="search">Optional text search.</param>
        /// <param name="actionType">Optional filter by action type.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Items per page.</param>
        /// <response code="200">Audit log entries returned.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("events/{eventId:guid}/audit-log")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAdminEventAuditLog(
            Guid eventId,
            [FromQuery] string? search,
            [FromQuery] string? actionType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (!IsAdmin()) return Forbid();

            var items = await _mediator.Send(new GetAdminEventAuditLogQuery
            {
                EventId = eventId,
                Search = search,
                ActionType = actionType,
                Page = page,
                PageSize = pageSize,
            }, cancellationToken);

            return Ok(items);
        }
    }
}

/// <summary>Payload for setting a vendor subscription tier.</summary>
public record SetVendorSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

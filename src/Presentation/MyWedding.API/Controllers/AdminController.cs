using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
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
        private readonly ApplicationDbContext _db;

        public AdminController(IMediator mediator, ApplicationDbContext db)
        {
            _mediator = mediator;
            _db = db;
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

        [HttpGet("commissions/payout-due")]
        public async Task<IActionResult> GetPayoutDue(CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var items = await _db.CommissionSettlements
                .AsNoTracking()
                .Where(c => !c.IsVendorPayoutSettled)
                .Join(_db.VendorBookings,
                    c => c.BookingId,
                    b => b.Id,
                    (c, b) => new
                    {
                        c.Id,
                        c.BookingId,
                        c.GrossAmount,
                        c.CommissionAmount,
                        c.VendorNetAmount,
                        c.CreatedAt,
                        b.ServiceId,
                        b.EventId
                    })
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpPatch("commissions/{settlementId:guid}/mark-settled")]
        public async Task<IActionResult> MarkSettlementAsPaid(Guid settlementId, CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var settlement = await _db.CommissionSettlements.FirstOrDefaultAsync(c => c.Id == settlementId, cancellationToken);
            if (settlement is null) return NotFound();

            settlement.IsVendorPayoutSettled = true;
            settlement.SettledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Settlement marked as paid." });
        }

        [HttpPatch("vendors/{vendorId}/subscription")]
        public async Task<IActionResult> SetVendorSubscription(string vendorId, [FromBody] SetVendorSubscriptionRequest request, CancellationToken cancellationToken)
        {
            if (!IsAdmin()) return Forbid();

            var hasVendor = await _db.Vendors.AnyAsync(v => v.UserId == vendorId, cancellationToken);
            if (!hasVendor) return NotFound(new { message = "Vendor not found." });

            var currentActive = await _db.VendorSubscriptions
                .Where(s => s.VendorId == vendorId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var existing in currentActive)
            {
                existing.Status = SubscriptionStatus.Cancelled;
                existing.EndsAt = DateTime.UtcNow;
            }

            await _db.VendorSubscriptions.AddAsync(new Domain.Entities.VendorSubscription
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                Tier = request.Tier,
                Status = SubscriptionStatus.Active,
                MonthlyFee = request.MonthlyFee,
                StartsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Vendor subscription updated." });
        }
    }
}

public record SetVendorSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

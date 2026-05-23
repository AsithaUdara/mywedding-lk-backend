using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.Vendors.Application.Features.Dashboard.Queries.GetVendorAnalytics;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Controller for vendor-specific dashboard operations including service management.
    /// </summary>
    [ApiController]
    [Route("api/vendor/dashboard")]
    [Authorize]
    public class VendorDashboardController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;

        public VendorDashboardController(IMediator mediator, ApplicationDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Retrieves all services owned by the current authenticated vendor.
        /// </summary>
        /// <returns>A list of vendor services.</returns>
        [HttpGet("services")]
        public async Task<IActionResult> GetMyServices()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetVendorServicesQuery { VendorId = userId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Adds a new service to the current vendor's profile.
        /// </summary>
        /// <param name="command">The service details.</param>
        /// <returns>The ID of the created service.</returns>
        [HttpPost("services")]
        public async Task<IActionResult> AddService([FromBody] AddServiceCommand command)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            command.VendorId = userId;

            if (command.CategoryId == Guid.Empty)
            {
                command.CategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Other
            }

            var result = await _mediator.Send(command);
            return Ok(new { id = result });
        }

        /// <summary>
        /// Updates an existing service owned by the vendor.
        /// </summary>
        /// <param name="id">The ID of the service to update.</param>
        /// <param name="command">The updated service details.</param>
        /// <returns>A success status or 404 if not found.</returns>
        [HttpPut("services/{id}")]
        public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceCommand command)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (command.CategoryId == Guid.Empty)
            {
                command.CategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Other
            }

            command.Id = id;
            var result = await _mediator.Send(command);
            return result ? Ok() : NotFound();
        }

        /// <summary>
        /// Deletes a service owned by the vendor.
        /// Note: Deletion is blocked if the service has active bookings.
        /// </summary>
        /// <param name="id">The ID of the service to delete.</param>
        /// <returns>A success status or error if deletion is restricted.</returns>
        [HttpDelete("services/{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var command = new DeleteServiceCommand { Id = id };
            var result = await _mediator.Send(command);
            return result ? Ok() : NotFound();
        }

        /// <summary>
        /// Retrieves analytics data for the vendor dashboard.
        /// </summary>
        /// <returns>Analytics data including bookings, earnings, and inquiries.</returns>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetVendorAnalyticsQuery { VendorId = userId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("subscription")]
        public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var sub = await _db.VendorSubscriptions
                .AsNoTracking()
                .Where(s => s.VendorId == userId && s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (sub is null)
            {
                return Ok(new { tier = SubscriptionPlanTier.Free.ToString(), monthlyFee = 0m, status = SubscriptionStatus.Active.ToString() });
            }

            return Ok(new { tier = sub.Tier.ToString(), monthlyFee = sub.MonthlyFee, status = sub.Status.ToString() });
        }

        [HttpPost("subscription")]
        public async Task<IActionResult> SetSubscription([FromBody] VendorSelfSubscriptionRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existing = await _db.VendorSubscriptions
                .Where(s => s.VendorId == userId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(cancellationToken);
            foreach (var sub in existing)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.EndsAt = DateTime.UtcNow;
            }

            await _db.VendorSubscriptions.AddAsync(new VendorSubscription
            {
                Id = Guid.NewGuid(),
                VendorId = userId,
                Tier = request.Tier,
                Status = SubscriptionStatus.Active,
                MonthlyFee = request.MonthlyFee,
                StartsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Vendor subscription updated.", tier = request.Tier.ToString() });
        }
    }
}

public record VendorSelfSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

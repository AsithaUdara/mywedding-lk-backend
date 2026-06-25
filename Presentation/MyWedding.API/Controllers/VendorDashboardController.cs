using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Infrastructure.Services;
using MyWedding.Vendors.Application.Features.Dashboard.Queries.GetVendorAnalytics;
using System.Security.Claims;

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
        private readonly IVendorDashboardService _vendorDashboard;

        public VendorDashboardController(IMediator mediator, IVendorDashboardService vendorDashboard)
        {
            _mediator = mediator;
            _vendorDashboard = vendorDashboard;
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
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ID of the created service.</returns>
        [HttpPost("services")]
        public async Task<IActionResult> AddService(
            [FromBody] AddServiceCommand command,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            command.VendorId = userId;

            if (command.CategoryId == Guid.Empty)
            {
                command.CategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Other
            }

            var verificationStatus = await _vendorDashboard.GetVendorVerificationStatusAsync(userId, cancellationToken);
            if (verificationStatus != VerificationStatus.Verified.ToString())
            {
                command.IsActive = false;
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { id = result });
        }

        /// <summary>
        /// Updates an existing service owned by the vendor.
        /// </summary>
        /// <param name="id">The ID of the service to update.</param>
        /// <param name="command">The updated service details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A success status or 404 if not found.</returns>
        [HttpPut("services/{id}")]
        public async Task<IActionResult> UpdateService(
            Guid id,
            [FromBody] UpdateServiceCommand command,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (command.CategoryId == Guid.Empty)
            {
                command.CategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Other
            }

            if (command.IsActive)
            {
                var verificationStatus = await _vendorDashboard.GetVendorVerificationStatusAsync(userId, cancellationToken);
                if (verificationStatus != VerificationStatus.Verified.ToString())
                {
                    return StatusCode(403, new
                    {
                        message = "Your account must be verified before publishing listings to the marketplace.",
                    });
                }
            }

            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);
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

        /// <summary>
        /// Returns the authenticated vendor's business profile (including pending verification).
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetBusinessProfile(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetBusinessProfileAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Updates location, contact, and public business details shown on the vendor listing.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateBusinessProfile(
            [FromBody] UpdateVendorBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.UpdateBusinessProfileAsync(GetUserId(), request, cancellationToken);
            return MapResult(result);
        }

        [HttpGet("subscription")]
        public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetSubscriptionAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        [HttpPost("subscription")]
        public async Task<IActionResult> SetSubscription(
            [FromBody] VendorSelfSubscriptionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.SetSubscriptionAsync(
                GetUserId(),
                (int)request.Tier,
                request.MonthlyFee,
                cancellationToken);
            return MapResult(result);
        }

        [HttpGet("billing-profile")]
        public async Task<IActionResult> GetBillingProfile(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetBillingProfileAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        [HttpPut("billing-profile")]
        public async Task<IActionResult> SaveBillingProfile(
            [FromBody] VendorBillingProfileRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.SaveBillingProfileAsync(GetUserId(), request, cancellationToken);
            return MapResult(result);
        }

        [HttpPost("subscription/checkout")]
        public async Task<IActionResult> CreateSubscriptionCheckout(
            [FromBody] VendorSelfSubscriptionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.CreateSubscriptionCheckoutAsync(
                GetUserId(),
                User.FindFirstValue(ClaimTypes.Email),
                (int)request.Tier,
                request.MonthlyFee,
                Request.Scheme,
                Request.Host.Value ?? string.Empty,
                cancellationToken);

            return MapResult(result);
        }

        private IActionResult MapResult(VendorDashboardWorkflowResult result) => result.StatusCode switch
        {
            200 => result.Body is null ? Ok() : Ok(result.Body),
            400 => BadRequest(result.Body),
            401 => result.Body is null ? Unauthorized() : Unauthorized(result.Body),
            403 => Forbid(),
            404 => result.Body is null ? NotFound() : NotFound(result.Body),
            _ => StatusCode(result.StatusCode, result.Body)
        };
    }
}

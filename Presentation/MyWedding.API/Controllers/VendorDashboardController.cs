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
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorDashboardController"/> class.
        /// </summary>
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
        /// <response code="200">Services returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("services")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// <response code="200">Service created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Vendor must be verified to publish active listings.</response>
        [HttpPost("services")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        /// <response code="200">Service updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Vendor must be verified to publish listings.</response>
        /// <response code="404">Service not found.</response>
        [HttpPut("services/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <response code="200">Service deleted.</response>
        /// <response code="404">Service not found or has active bookings.</response>
        [HttpDelete("services/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <response code="200">Analytics returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("analytics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// <response code="200">Profile returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBusinessProfile(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetBusinessProfileAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Updates location, contact, and public business details shown on the vendor listing.
        /// </summary>
        /// <response code="200">Profile updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateBusinessProfile(
            [FromBody] UpdateVendorBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.UpdateBusinessProfileAsync(GetUserId(), request, cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Returns the vendor's current subscription tier and billing status.
        /// </summary>
        /// <response code="200">Subscription returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetSubscriptionAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Updates the vendor's subscription tier (self-service).
        /// </summary>
        /// <param name="request">Subscription tier and monthly fee.</param>
        /// <response code="200">Subscription updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Returns the vendor's billing profile for invoices and payments.
        /// </summary>
        /// <response code="200">Billing profile returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("billing-profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBillingProfile(CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.GetBillingProfileAsync(GetUserId(), cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Saves or updates the vendor's billing profile.
        /// </summary>
        /// <param name="request">Company name, address, and tax details.</param>
        /// <response code="200">Billing profile saved.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPut("billing-profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SaveBillingProfile(
            [FromBody] VendorBillingProfileRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _vendorDashboard.SaveBillingProfileAsync(GetUserId(), request, cancellationToken);
            return MapResult(result);
        }

        /// <summary>
        /// Creates a PayHere checkout session for a vendor subscription upgrade.
        /// </summary>
        /// <param name="request">Subscription tier and monthly fee.</param>
        /// <response code="200">Checkout session created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("subscription/checkout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

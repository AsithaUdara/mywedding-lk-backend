using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Infrastructure.Services;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Payment workflows: deposit checkout, planner subscriptions, and PayHere webhooks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentWorkflowService _paymentWorkflow;
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentsController"/> class.
        /// </summary>
    public PaymentsController(IPaymentWorkflowService paymentWorkflow)
    {
        _paymentWorkflow = paymentWorkflow;
    }

    /// <summary>
    /// Creates a PayHere checkout session for a booking deposit.
    /// </summary>
    /// <param name="bookingId">The booking to pay a deposit for.</param>
    /// <returns>Checkout URL and payment metadata.</returns>
    /// <response code="200">Checkout session created.</response>
    /// <response code="400">Invalid booking or payment state.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not authorized for this booking.</response>
    /// <response code="404">Booking not found.</response>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/deposit-checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDepositCheckout(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _paymentWorkflow.CreateDepositCheckoutAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue(ClaimTypes.Email),
            bookingId,
            Request.Scheme,
            Request.Host.Value ?? string.Empty,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Creates a PayHere checkout session for a planner subscription tier.
    /// </summary>
    /// <param name="request">Subscription tier and monthly fee.</param>
    /// <returns>Checkout URL and payment metadata.</returns>
    /// <response code="200">Checkout session created.</response>
    /// <response code="400">Invalid subscription request.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [Authorize]
    [HttpPost("planner-subscription/checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePlannerSubscriptionCheckout(
        [FromBody] PlannerSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentWorkflow.CreatePlannerSubscriptionCheckoutAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue(ClaimTypes.Email),
            (int)request.Tier,
            request.MonthlyFee,
            Request.Scheme,
            Request.Host.Value ?? string.Empty,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Handles PayHere payment gateway webhooks (server-to-server).
    /// </summary>
    /// <param name="request">PayHere notification payload.</param>
    /// <returns>Acknowledgement of webhook processing.</returns>
    /// <response code="200">Webhook processed successfully.</response>
    /// <response code="400">Invalid webhook payload.</response>
    [AllowAnonymous]
    [HttpPost("payhere/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandlePayHereWebhook(
        [FromForm] PayHereWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentWorkflow.HandlePayHereWebhookAsync(request, cancellationToken);
        return MapResult(result);
    }

    /// <summary>
    /// Returns the current payment status for a booking deposit.
    /// </summary>
    /// <param name="bookingId">The booking to check.</param>
    /// <returns>Payment status and transaction details.</returns>
    /// <response code="200">Payment status returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not authorized for this booking.</response>
    /// <response code="404">Booking not found.</response>
    [Authorize]
    [HttpGet("bookings/{bookingId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingPaymentStatus(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _paymentWorkflow.GetBookingPaymentStatusAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole("admin"),
            bookingId,
            cancellationToken);

        return MapResult(result);
    }

    private IActionResult MapResult(PaymentWorkflowResult result) => result.StatusCode switch
    {
        200 => result.Body is null ? Ok() : Ok(result.Body),
        400 => BadRequest(result.Body),
        401 => result.Body is null ? Unauthorized() : Unauthorized(result.Body),
        403 => Forbid(),
        404 => result.Body is null ? NotFound() : NotFound(result.Body),
        _ => StatusCode(result.StatusCode, result.Body)
    };
}

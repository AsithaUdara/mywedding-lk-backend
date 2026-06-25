using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Infrastructure.Services;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentWorkflowService _paymentWorkflow;

    public PaymentsController(IPaymentWorkflowService paymentWorkflow)
    {
        _paymentWorkflow = paymentWorkflow;
    }

    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/deposit-checkout")]
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

    [Authorize]
    [HttpPost("planner-subscription/checkout")]
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

    [AllowAnonymous]
    [HttpPost("payhere/webhook")]
    public async Task<IActionResult> HandlePayHereWebhook(
        [FromForm] PayHereWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentWorkflow.HandlePayHereWebhookAsync(request, cancellationToken);
        return MapResult(result);
    }

    [Authorize]
    [HttpGet("bookings/{bookingId:guid}/status")]
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

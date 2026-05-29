using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        ApplicationDbContext db,
        IPaymentGatewayService paymentGateway,
        IConfiguration configuration)
    {
        _db = db;
        _paymentGateway = paymentGateway;
        _configuration = configuration;
    }

    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/deposit-checkout")]
    public async Task<IActionResult> CreateDepositCheckout(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var booking = await _db.VendorBookings
            .Include(b => b.VendorService)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return NotFound(new { message = "Booking not found." });

        var hasEventAccess = booking.BookedById == userId || await _db.EventOrganizers
            .AnyAsync(o => o.EventId == booking.EventId && o.UserId == userId, cancellationToken);
        if (!hasEventAccess)
            return Forbid();

        var notifyUrl = _configuration["PayHere:NotifyUrl"]
            ?? $"{Request.Scheme}://{Request.Host}/api/payments/payhere/webhook";
        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var vendorsReturnPath =
            $"/events/{booking.EventId}/vendors?bookingId={bookingId}&payment=return";
        var vendorsCancelPath =
            $"/events/{booking.EventId}/vendors?bookingId={bookingId}&payment=cancel";
        var returnUrl = _configuration["PayHere:ReturnUrl"];
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl.Contains("/dashboard", StringComparison.OrdinalIgnoreCase))
            returnUrl = $"{frontendBase}{vendorsReturnPath}";
        var cancelUrl = _configuration["PayHere:CancelUrl"];
        if (string.IsNullOrWhiteSpace(cancelUrl) || cancelUrl.Contains("/dashboard", StringComparison.OrdinalIgnoreCase))
            cancelUrl = $"{frontendBase}{vendorsCancelPath}";

        var result = await _paymentGateway.ProcessSplitPaymentAsync(
            new SplitPaymentRequest(
                bookingId,
                booking.VendorService?.VendorId ?? string.Empty,
                booking.FinalAmount,
                PayerEmail: User.FindFirstValue(ClaimTypes.Email) ?? "client@mywedding.lk",
                NotifyUrl: notifyUrl,
                ReturnUrl: returnUrl,
                CancelUrl: cancelUrl),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (result.Status == "already_paid")
            return Ok(new { message = result.Message, alreadyPaid = true, bookingId });

        return Ok(new
        {
            bookingId,
            transactionId = result.PaymentTransactionId,
            grossAmountLkr = result.GrossAmountLkr,
            platformCommissionLkr = result.PlatformCommissionLkr,
            vendorNetPayoutLkr = result.VendorNetPayoutLkr,
            isSimulated = result.IsSimulated,
            checkout = result.CheckoutForm
        });
    }

    [AllowAnonymous]
    [HttpPost("payhere/webhook")]
    public async Task<IActionResult> HandlePayHereWebhook([FromForm] PayHereWebhookRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.order_id, out var orderId))
            return BadRequest(new { message = "Invalid order_id." });

        var subscriptionCheckout = await _db.VendorSubscriptionCheckouts
            .FirstOrDefaultAsync(c => c.Id == orderId, cancellationToken);
        if (subscriptionCheckout is not null)
        {
            if (!IsWebhookSignatureValid(request))
                return Unauthorized(new { message = "Invalid webhook signature." });

            return await HandleVendorSubscriptionWebhook(subscriptionCheckout, request, cancellationToken);
        }

        var webhookResult = await _paymentGateway.ProcessPayHereWebhookAsync(
            new PayHereWebhookNotification(
                request.merchant_id,
                request.order_id,
                request.payment_id,
                request.payhere_amount,
                request.payhere_currency,
                request.status_code,
                request.md5sig,
                request.method),
            cancellationToken);

        if (!webhookResult.Success && webhookResult.Message.Contains("signature", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = webhookResult.Message });

        if (!webhookResult.Success)
            return webhookResult.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { message = webhookResult.Message })
                : BadRequest(new { message = webhookResult.Message });

        return Ok(new { message = webhookResult.Message });
    }

    [Authorize]
    [HttpGet("bookings/{bookingId:guid}/status")]
    public async Task<IActionResult> GetBookingPaymentStatus(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var booking = await _db.VendorBookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return NotFound();

        var hasEventAccess = booking.BookedById == userId || await _db.EventOrganizers
            .AnyAsync(o => o.EventId == booking.EventId && o.UserId == userId, cancellationToken);
        if (!hasEventAccess)
            return Forbid();

        var tx = await _db.BookingPaymentTransactions
            .Where(t => t.BookingId == bookingId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            bookingId,
            bookingStatus = booking.Status.ToString(),
            paymentStatus = tx?.Status.ToString() ?? "None",
            paidAt = tx?.PaidAt
        });
    }

    private async Task<IActionResult> HandleVendorSubscriptionWebhook(
        VendorSubscriptionCheckout checkout,
        PayHereWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (checkout.Status == "Paid")
            return Ok(new { message = "Subscription payment already processed." });

        if (request.status_code == "2")
        {
            checkout.Status = "Paid";
            checkout.PaidAt = DateTime.UtcNow;

            var existing = await _db.VendorSubscriptions
                .Where(s => s.VendorId == checkout.VendorId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(cancellationToken);
            foreach (var sub in existing)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.EndsAt = DateTime.UtcNow;
            }

            await _db.VendorSubscriptions.AddAsync(new VendorSubscription
            {
                Id = Guid.NewGuid(),
                VendorId = checkout.VendorId,
                Tier = checkout.Tier,
                Status = SubscriptionStatus.Active,
                MonthlyFee = checkout.Amount,
                StartsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            }, cancellationToken);

            var profile = await _db.VendorBillingProfiles
                .FirstOrDefaultAsync(p => p.VendorId == checkout.VendorId, cancellationToken);
            if (profile is null)
            {
                profile = new VendorBillingProfile { VendorId = checkout.VendorId };
                await _db.VendorBillingProfiles.AddAsync(profile, cancellationToken);
            }

            profile.PayHerePaymentMethod = request.method;
            profile.CardBrand = InferCardBrand(request.method);
            profile.Last4 ??= "0000";
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Vendor subscription payment processed." });
        }

        checkout.Status = "Failed";
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Vendor subscription payment failed." });
    }

    private static string? InferCardBrand(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return null;
        var m = method.ToUpperInvariant();
        if (m.Contains("VISA")) return "Visa";
        if (m.Contains("MASTER")) return "Mastercard";
        return method;
    }

    private bool IsWebhookSignatureValid(PayHereWebhookRequest request)
    {
        var merchantSecret = _configuration["PayHere:MerchantSecret"] ?? string.Empty;
        return MyWedding.Infrastructure.Payments.PayHereHashHelper.IsWebhookSignatureValid(
            new PayHereWebhookNotification(
                request.merchant_id,
                request.order_id,
                request.payment_id,
                request.payhere_amount,
                request.payhere_currency,
                request.status_code,
                request.md5sig,
                request.method),
            merchantSecret);
    }
}

public record PayHereWebhookRequest(
    string merchant_id,
    string order_id,
    string payment_id,
    string payhere_amount,
    string payhere_currency,
    string status_code,
    string md5sig,
    string method
);

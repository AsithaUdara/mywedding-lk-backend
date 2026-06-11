using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Payments;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;
using System.Globalization;
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

        var shortlistLinked = await _db.VendorShortlistItems
            .AnyAsync(i => i.VendorBookingId == bookingId, cancellationToken);
        if (shortlistLinked)
        {
            // Allow retries while booking is AwaitingPayment (set on first checkout attempt).
            // Block only when the client has not completed e-sign yet.
            var contractSigned = await _db.BookingContracts
                .AsNoTracking()
                .AnyAsync(
                    c => c.Id == bookingId && c.ClientSignedAt != null,
                    cancellationToken);
            if (!contractSigned)
            {
                return BadRequest(new
                {
                    message = "Please sign the vendor contract before paying the deposit.",
                    code = "contract_signature_required"
                });
            }
        }

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

    [Authorize]
    [HttpPost("planner-subscription/checkout")]
    public async Task<IActionResult> CreatePlannerSubscriptionCheckout(
        [FromBody] PlannerSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var plannerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        if (request.Tier != SubscriptionPlanTier.PlannerPro || request.MonthlyFee <= 0)
            return BadRequest(new { message = "Checkout is only required for Planner Pro." });

        var planner = await _db.WeddingPlanners
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == plannerId, cancellationToken);
        if (planner is null)
            return NotFound(new { message = "Planner profile not found." });

        var checkout = new PlannerSubscriptionCheckout
        {
            Id = Guid.NewGuid(),
            PlannerId = plannerId,
            Tier = request.Tier,
            Amount = request.MonthlyFee,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };
        await _db.PlannerSubscriptionCheckouts.AddAsync(checkout, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var sandboxUrl = _configuration["PayHere:SandboxCheckoutUrl"] ?? "https://sandbox.payhere.lk/pay/checkout";
        var merchantId = _configuration["PayHere:MerchantId"] ?? "TEST_MERCHANT";
        var notifyUrl = _configuration["PayHere:NotifyUrl"]
            ?? $"{Request.Scheme}://{Request.Host}/api/payments/payhere/webhook";
        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var returnUrl = _configuration["PayHere:PlannerSubscriptionReturnUrl"]
            ?? $"{frontendBase}/planner/billing?payment=success";
        var cancelUrl = _configuration["PayHere:PlannerSubscriptionCancelUrl"]
            ?? $"{frontendBase}/planner/billing?payment=cancelled";

        var orderId = checkout.Id.ToString();
        var currency = "LKR";
        var merchantSecret = _configuration["PayHere:MerchantSecret"] ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? planner.User?.Email ?? "planner@mywedding.lk";
        var checkoutPayload = PayHereCheckoutFormBuilder.Build(
            sandboxUrl,
            merchantId,
            merchantSecret,
            orderId,
            request.MonthlyFee,
            currency,
            $"Planner {request.Tier} Plan",
            returnUrl,
            cancelUrl,
            notifyUrl,
            email,
            firstName: planner.User?.FirstName ?? planner.BusinessName,
            lastName: planner.User?.LastName ?? "Planner",
            phone: planner.ContactPhone,
            city: planner.City);

        return Ok(new
        {
            checkoutId = checkout.Id,
            checkout = checkoutPayload,
        });
    }

    [AllowAnonymous]
    [HttpPost("payhere/webhook")]
    public async Task<IActionResult> HandlePayHereWebhook([FromForm] PayHereWebhookRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.order_id, out var orderId))
            return BadRequest(new { message = "Invalid order_id." });

        var vendorSubscriptionCheckout = await _db.VendorSubscriptionCheckouts
            .FirstOrDefaultAsync(c => c.Id == orderId, cancellationToken);
        if (vendorSubscriptionCheckout is not null)
        {
            if (!IsWebhookSignatureValid(request))
                return Unauthorized(new { message = "Invalid webhook signature." });

            return await HandleVendorSubscriptionWebhook(vendorSubscriptionCheckout, request, cancellationToken);
        }

        var plannerSubscriptionCheckout = await _db.PlannerSubscriptionCheckouts
            .FirstOrDefaultAsync(c => c.Id == orderId, cancellationToken);
        if (plannerSubscriptionCheckout is not null)
        {
            if (!IsWebhookSignatureValid(request))
                return Unauthorized(new { message = "Invalid webhook signature." });

            return await HandlePlannerSubscriptionWebhook(plannerSubscriptionCheckout, request, cancellationToken);
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

        var isAdmin = User.IsInRole("admin");
        var hasEventAccess = booking.BookedById == userId
            || await _db.EventOrganizers.AnyAsync(
                o => o.EventId == booking.EventId && o.UserId == userId,
                cancellationToken)
            || await _db.PlannerClientEvents.AnyAsync(
                e => e.EventId == booking.EventId && e.PlannerId == userId,
                cancellationToken);
        if (!isAdmin && !hasEventAccess)
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

    private async Task<IActionResult> HandlePlannerSubscriptionWebhook(
        PlannerSubscriptionCheckout checkout,
        PayHereWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (checkout.Status == "Paid")
            return Ok(new { message = "Planner subscription payment already processed." });

        if (request.status_code == "2")
        {
            checkout.Status = "Paid";
            checkout.PaidAt = DateTime.UtcNow;

            var activeSubs = await _db.PlannerSubscriptions
                .Where(s => s.PlannerId == checkout.PlannerId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var sub in activeSubs)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.EndsAt = DateTime.UtcNow;
            }

            var maxConcurrentEvents = checkout.Tier == SubscriptionPlanTier.PlannerPro ? 10 : 1;

            var periodStart = DateTime.UtcNow;
            await _db.PlannerSubscriptions.AddAsync(new PlannerSubscription
            {
                Id = Guid.NewGuid(),
                PlannerId = checkout.PlannerId,
                Tier = checkout.Tier,
                Status = SubscriptionStatus.Active,
                MonthlyFee = checkout.Amount,
                MaxConcurrentEvents = maxConcurrentEvents,
                StartsAt = periodStart,
                EndsAt = checkout.Tier == SubscriptionPlanTier.PlannerPro
                    ? periodStart.AddMonths(1)
                    : null,
                CreatedAt = periodStart,
            }, cancellationToken);

            await UpsertPlannerBillingProfileAsync(
                checkout.PlannerId,
                request,
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Planner subscription payment processed." });
        }

        checkout.Status = "Failed";
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Planner subscription payment failed." });
    }

    private async Task UpsertPlannerBillingProfileAsync(
        string plannerId,
        PayHereWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _db.PlannerBillingProfiles
            .FirstOrDefaultAsync(p => p.PlannerId == plannerId, cancellationToken);

        if (profile is null)
        {
            profile = new PlannerBillingProfile { PlannerId = plannerId };
            await _db.PlannerBillingProfiles.AddAsync(profile, cancellationToken);
        }

        profile.PayHerePaymentMethod = request.method;
        profile.CardBrand = InferCardBrand(request.method);
        profile.CardholderName = string.IsNullOrWhiteSpace(request.card_holder_name)
            ? profile.CardholderName
            : request.card_holder_name.Trim();

        var last4 = PayHereCardMetadataHelper.ExtractLast4(request.card_no);
        if (!string.IsNullOrWhiteSpace(last4))
            profile.Last4 = last4;

        var (month, year) = PayHereCardMetadataHelper.ParseCardExpiry(request.card_expiry);
        if (month.HasValue)
            profile.ExpiryMonth = month;
        if (year.HasValue)
            profile.ExpiryYear = year;

        profile.UpdatedAt = DateTime.UtcNow;
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

public record PlannerSubscriptionCheckoutRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

public record PayHereWebhookRequest(
    string merchant_id,
    string order_id,
    string payment_id,
    string payhere_amount,
    string payhere_currency,
    string status_code,
    string md5sig,
    string method,
    string? card_holder_name = null,
    string? card_no = null,
    string? card_expiry = null
);

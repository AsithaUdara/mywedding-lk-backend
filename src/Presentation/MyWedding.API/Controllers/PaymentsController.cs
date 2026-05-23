using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private static readonly Guid OtherBudgetCategoryId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public PaymentsController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
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
            .Include(b => b.WeddingEvent)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return NotFound(new { message = "Booking not found." });

        var hasEventAccess = booking.BookedById == userId || await _db.EventOrganizers
            .AnyAsync(o => o.EventId == booking.EventId && o.UserId == userId, cancellationToken);
        if (!hasEventAccess)
            return Forbid();

        var existingPaid = await _db.BookingPaymentTransactions
            .FirstOrDefaultAsync(t => t.BookingId == bookingId && t.Status == PaymentTransactionStatus.Paid, cancellationToken);
        if (existingPaid is not null)
            return Ok(new { message = "Booking already paid.", alreadyPaid = true });

        var transaction = await _db.BookingPaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.BookingId == bookingId, cancellationToken);

        if (transaction is null || transaction.Status == PaymentTransactionStatus.Failed)
        {
            transaction = new BookingPaymentTransaction
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                GatewayName = "PayHere",
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                Amount = booking.FinalAmount,
                Currency = "LKR",
                Status = PaymentTransactionStatus.Initiated,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.BookingPaymentTransactions.AddAsync(transaction, cancellationToken);
        }

        booking.Status = BookingStatus.AwaitingPayment;
        transaction.Status = PaymentTransactionStatus.Pending;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var sandboxUrl = _configuration["PayHere:SandboxCheckoutUrl"] ?? "https://sandbox.payhere.lk/pay/checkout";
        var merchantId = _configuration["PayHere:MerchantId"] ?? "TEST_MERCHANT";
        var notifyUrl = _configuration["PayHere:NotifyUrl"] ?? $"{Request.Scheme}://{Request.Host}/api/payments/payhere/webhook";
        var returnUrl = _configuration["PayHere:ReturnUrl"] ?? $"{_configuration["Frontend:BaseUrl"]}/events/{booking.EventId}";
        var cancelUrl = _configuration["PayHere:CancelUrl"] ?? returnUrl;

        return Ok(new
        {
            bookingId,
            transactionId = transaction.Id,
            checkout = new
            {
                checkoutUrl = sandboxUrl,
                merchant_id = merchantId,
                return_url = returnUrl,
                cancel_url = cancelUrl,
                notify_url = notifyUrl,
                order_id = bookingId,
                items = "Vendor Deposit",
                amount = booking.FinalAmount,
                currency = "LKR",
                first_name = "Wedding",
                last_name = "Client",
                email = User.FindFirstValue(ClaimTypes.Email) ?? "client@mywedding.lk"
            }
        });
    }

    [AllowAnonymous]
    [HttpPost("payhere/webhook")]
    public async Task<IActionResult> HandlePayHereWebhook([FromForm] PayHereWebhookRequest request, CancellationToken cancellationToken)
    {
        if (!IsWebhookSignatureValid(request))
        {
            return Unauthorized(new { message = "Invalid webhook signature." });
        }

        if (!Guid.TryParse(request.order_id, out var bookingId))
            return BadRequest(new { message = "Invalid order_id." });

        var booking = await _db.VendorBookings
            .Include(b => b.VendorService)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return NotFound();

        var tx = await _db.BookingPaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.BookingId == bookingId, cancellationToken);
        if (tx is null)
            return NotFound();

        if (tx.Status == PaymentTransactionStatus.Paid)
            return Ok(new { message = "Already processed." });

        tx.GatewayPaymentId = request.payment_id;
        tx.RawCallbackPayload = JsonSerializer.Serialize(request);
        tx.UpdatedAt = DateTime.UtcNow;

        if (request.status_code == "2")
        {
            tx.Status = PaymentTransactionStatus.Paid;
            tx.PaidAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Confirmed;

            var hasExpense = await _db.Expenses.AnyAsync(
                e => e.EventId == booking.EventId && e.Title == $"Vendor Deposit - {booking.Id}",
                cancellationToken);
            if (!hasExpense)
            {
                await _db.Expenses.AddAsync(new Expense
                {
                    Id = Guid.NewGuid(),
                    EventId = booking.EventId,
                    Title = $"Vendor Deposit - {booking.Id}",
                    Amount = booking.FinalAmount,
                    ExpenseDate = DateTime.UtcNow,
                    BudgetCategoryId = OtherBudgetCategoryId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            var hasSettlement = await _db.CommissionSettlements.AnyAsync(c => c.BookingId == booking.Id, cancellationToken);
            if (!hasSettlement)
            {
                var commission = Math.Round(booking.FinalAmount * 0.05m, 2, MidpointRounding.AwayFromZero);
                await _db.CommissionSettlements.AddAsync(new CommissionSettlement
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    GrossAmount = booking.FinalAmount,
                    CommissionAmount = commission,
                    VendorNetAmount = booking.FinalAmount - commission,
                    CommissionRate = 0.05m,
                    IsVendorPayoutSettled = false,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }
        else
        {
            tx.Status = PaymentTransactionStatus.Failed;
            booking.Status = BookingStatus.Requested;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Webhook processed." });
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

    private bool IsWebhookSignatureValid(PayHereWebhookRequest request)
    {
        var merchantSecret = _configuration["PayHere:MerchantSecret"];
        if (string.IsNullOrWhiteSpace(merchantSecret))
        {
            // For local sandbox until secrets are configured.
            return true;
        }

        // Lightweight validation path for sandbox callbacks.
        var raw = $"{request.merchant_id}{request.order_id}{request.payhere_amount}{request.payhere_currency}{request.status_code}{merchantSecret}";
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        var generated = Convert.ToHexString(hash);
        return string.Equals(generated, request.md5sig, StringComparison.OrdinalIgnoreCase);
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

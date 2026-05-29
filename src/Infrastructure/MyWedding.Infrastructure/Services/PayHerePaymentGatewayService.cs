using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Payments;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class PayHerePaymentGatewayService : IPaymentGatewayService
{
    private static readonly Guid OtherBudgetCategoryId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayHerePaymentGatewayService> _logger;

    public PayHerePaymentGatewayService(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger<PayHerePaymentGatewayService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SplitPaymentResult> ProcessSplitPaymentAsync(
        SplitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.GrossAmountLkr <= 0)
        {
            return FailedResult(request, "Gross amount must be greater than zero.");
        }

        var booking = await _db.VendorBookings
            .Include(b => b.VendorService)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return FailedResult(request, "Booking not found.");

        var vendorUserId = booking.VendorService?.VendorId ?? request.VendorUserId;
        if (!string.IsNullOrWhiteSpace(request.VendorUserId) &&
            !string.Equals(vendorUserId, request.VendorUserId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "VendorUserId mismatch for booking {BookingId}: request={RequestVendor}, actual={ActualVendor}",
                request.BookingId,
                request.VendorUserId,
                vendorUserId);
        }

        var existingPaid = await _db.BookingPaymentTransactions
            .AnyAsync(t => t.BookingId == request.BookingId && t.Status == PaymentTransactionStatus.Paid, cancellationToken);
        if (existingPaid)
        {
            await SyncShortlistDepositPaidAsync(request.BookingId, cancellationToken);
            if (booking.Status == BookingStatus.AwaitingPayment)
            {
                booking.Status = BookingStatus.Confirmed;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new SplitPaymentResult(
                Success: true,
                GatewayName: "PayHere",
                GatewayPaymentId: string.Empty,
                GrossAmountLkr: request.GrossAmountLkr,
                PlatformCommissionLkr: 0,
                VendorNetPayoutLkr: request.GrossAmountLkr,
                PlatformTakeRateApplied: request.PlatformTakeRate,
                Status: "already_paid",
                Message: "Booking deposit already paid.",
                IsSimulated: false);
        }

        var takeRate = request.PlatformTakeRate is > 0 and <= 1 ? request.PlatformTakeRate : 0.05m;
        var commission = Math.Round(request.GrossAmountLkr * takeRate, 2, MidpointRounding.AwayFromZero);
        var vendorNet = request.GrossAmountLkr - commission;

        var transaction = await _db.BookingPaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.BookingId == request.BookingId, cancellationToken);

        if (transaction is null || transaction.Status == PaymentTransactionStatus.Failed)
        {
            transaction = new BookingPaymentTransaction
            {
                Id = Guid.NewGuid(),
                BookingId = request.BookingId,
                GatewayName = "PayHere",
                IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString("N"),
                Amount = request.GrossAmountLkr,
                Currency = request.Currency,
                Status = PaymentTransactionStatus.Initiated,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.BookingPaymentTransactions.AddAsync(transaction, cancellationToken);
        }

        booking.Status = BookingStatus.AwaitingPayment;
        transaction.Status = PaymentTransactionStatus.Pending;
        transaction.Amount = request.GrossAmountLkr;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var checkout = BuildCheckoutForm(
            request.BookingId,
            request.GrossAmountLkr,
            request.Currency,
            request.PayerEmail,
            request.NotifyUrl,
            request.ReturnUrl,
            request.CancelUrl);

        return new SplitPaymentResult(
            Success: true,
            GatewayName: "PayHere",
            GatewayPaymentId: string.Empty,
            GrossAmountLkr: request.GrossAmountLkr,
            PlatformCommissionLkr: commission,
            VendorNetPayoutLkr: vendorNet,
            PlatformTakeRateApplied: takeRate,
            Status: "pending_checkout",
            Message: "Redirect the client to PayHere checkout to complete the deposit.",
            IsSimulated: false,
            PaymentTransactionId: transaction.Id,
            CheckoutForm: checkout);
    }

    public async Task<PayHereWebhookProcessResult> ProcessPayHereWebhookAsync(
        PayHereWebhookNotification notification,
        CancellationToken cancellationToken = default)
    {
        var merchantSecret = _configuration["PayHere:MerchantSecret"] ?? string.Empty;
        if (!PayHereHashHelper.IsWebhookSignatureValid(notification, merchantSecret))
        {
            return new PayHereWebhookProcessResult(false, false, false, "Invalid webhook signature.");
        }

        if (!Guid.TryParse(notification.OrderId, out var orderId))
            return new PayHereWebhookProcessResult(false, false, false, "Invalid order_id.");

        var subscriptionCheckout = await _db.VendorSubscriptionCheckouts
            .FirstOrDefaultAsync(c => c.Id == orderId, cancellationToken);
        if (subscriptionCheckout is not null)
        {
            return new PayHereWebhookProcessResult(
                true,
                subscriptionCheckout.Status == "Paid",
                IsSubscriptionPayment: true,
                "Subscription webhook should be handled by PaymentsController.");
        }

        var booking = await _db.VendorBookings
            .Include(b => b.VendorService)
            .FirstOrDefaultAsync(b => b.Id == orderId, cancellationToken);
        if (booking is null)
            return new PayHereWebhookProcessResult(false, false, false, "Booking not found.");

        var tx = await _db.BookingPaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.BookingId == orderId, cancellationToken);
        if (tx is null)
            return new PayHereWebhookProcessResult(false, false, false, "Payment transaction not found.");

        if (tx.Status == PaymentTransactionStatus.Paid)
        {
            return new PayHereWebhookProcessResult(true, true, false, "Already processed.");
        }

        tx.GatewayPaymentId = notification.PaymentId;
        tx.RawCallbackPayload = JsonSerializer.Serialize(notification);
        tx.UpdatedAt = DateTime.UtcNow;

        if (notification.StatusCode == "2")
        {
            tx.Status = PaymentTransactionStatus.Paid;
            tx.PaidAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Confirmed;

            await SyncShortlistDepositPaidAsync(booking.Id, cancellationToken);
            await EnsureDepositExpenseAsync(booking, cancellationToken);
            await EnsureCommissionSettlementAsync(booking, cancellationToken);
        }
        else
        {
            tx.Status = PaymentTransactionStatus.Failed;
            booking.Status = BookingStatus.Requested;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new PayHereWebhookProcessResult(true, false, false, "Webhook processed.");
    }

    private async Task SyncShortlistDepositPaidAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var shortlistItems = await _db.VendorShortlistItems
            .Where(i => i.VendorBookingId == bookingId)
            .ToListAsync(cancellationToken);

        foreach (var item in shortlistItems)
        {
            item.Status = VendorShortlistItemStatus.DepositPaid;
            item.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task EnsureCommissionSettlementAsync(VendorBooking booking, CancellationToken cancellationToken)
    {
        var hasSettlement = await _db.CommissionSettlements
            .AnyAsync(c => c.BookingId == booking.Id, cancellationToken);
        if (hasSettlement)
            return;

        var takeRate = 0.05m;
        var commission = Math.Round(booking.FinalAmount * takeRate, 2, MidpointRounding.AwayFromZero);
        await _db.CommissionSettlements.AddAsync(new CommissionSettlement
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            GrossAmount = booking.FinalAmount,
            CommissionAmount = commission,
            VendorNetAmount = booking.FinalAmount - commission,
            CommissionRate = takeRate,
            IsVendorPayoutSettled = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsureDepositExpenseAsync(VendorBooking booking, CancellationToken cancellationToken)
    {
        var hasExpense = await _db.Expenses.AnyAsync(
            e => e.EventId == booking.EventId && e.Title == $"Vendor Deposit - {booking.Id}",
            cancellationToken);
        if (hasExpense)
            return;

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

    private IReadOnlyDictionary<string, object> BuildCheckoutForm(
        Guid bookingId,
        decimal amount,
        string currency,
        string? payerEmail,
        string? notifyUrl,
        string? returnUrl,
        string? cancelUrl)
    {
        var sandboxUrl = _configuration["PayHere:SandboxCheckoutUrl"] ?? "https://sandbox.payhere.lk/pay/checkout";
        var merchantId = _configuration["PayHere:MerchantId"] ?? "TEST_MERCHANT";
        var merchantSecret = _configuration["PayHere:MerchantSecret"] ?? "";
        var frontendBase = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";

        notifyUrl ??= _configuration["PayHere:NotifyUrl"];
        returnUrl ??= _configuration["PayHere:ReturnUrl"] ?? $"{frontendBase}/dashboard";
        cancelUrl ??= _configuration["PayHere:CancelUrl"] ?? returnUrl;

        var orderId = bookingId.ToString();
        var checkout = new Dictionary<string, object>
        {
            ["checkoutUrl"] = sandboxUrl,
            ["merchant_id"] = merchantId,
            ["return_url"] = returnUrl,
            ["cancel_url"] = cancelUrl,
            ["notify_url"] = notifyUrl ?? string.Empty,
            ["order_id"] = orderId,
            ["items"] = "Vendor Deposit",
            ["amount"] = amount,
            ["currency"] = currency,
            ["first_name"] = "Wedding",
            ["last_name"] = "Client",
            ["email"] = payerEmail ?? "client@mywedding.lk"
        };

        if (!string.IsNullOrWhiteSpace(merchantSecret))
        {
            checkout["hash"] = PayHereHashHelper.BuildCheckoutHash(
                merchantId, orderId, amount, currency, merchantSecret);
        }

        return checkout;
    }

    private static SplitPaymentResult FailedResult(SplitPaymentRequest request, string message) =>
        new(
            Success: false,
            GatewayName: "PayHere",
            GatewayPaymentId: string.Empty,
            GrossAmountLkr: request.GrossAmountLkr,
            PlatformCommissionLkr: 0,
            VendorNetPayoutLkr: 0,
            PlatformTakeRateApplied: request.PlatformTakeRate,
            Status: "failed",
            Message: message,
            IsSimulated: false);
}

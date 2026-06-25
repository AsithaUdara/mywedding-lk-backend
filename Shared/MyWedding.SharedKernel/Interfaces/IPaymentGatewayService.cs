namespace MyWedding.SharedKernel.Interfaces;

/// <summary>
/// Payment gateway abstraction for booking deposits and platform commission splits (PayHere in production).
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Initiates a split deposit payment: persists a gateway transaction and returns PayHere checkout fields.
    /// </summary>
    Task<SplitPaymentResult> ProcessSplitPaymentAsync(
        SplitPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a PayHere notify callback, updates the transaction, and creates commission settlement on success.
    /// </summary>
    Task<PayHereWebhookProcessResult> ProcessPayHereWebhookAsync(
        PayHereWebhookNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record SplitPaymentRequest(
    Guid BookingId,
    string VendorUserId,
    decimal GrossAmountLkr,
    decimal PlatformTakeRate = 0.05m,
    string Currency = "LKR",
    string? IdempotencyKey = null,
    string? PayerEmail = null,
    string? NotifyUrl = null,
    string? ReturnUrl = null,
    string? CancelUrl = null);

public sealed record SplitPaymentResult(
    bool Success,
    string GatewayName,
    string GatewayPaymentId,
    decimal GrossAmountLkr,
    decimal PlatformCommissionLkr,
    decimal VendorNetPayoutLkr,
    decimal PlatformTakeRateApplied,
    string Status,
    string? Message,
    bool IsSimulated,
    Guid? PaymentTransactionId = null,
    IReadOnlyDictionary<string, object>? CheckoutForm = null);

public sealed record PayHereWebhookNotification(
    string MerchantId,
    string OrderId,
    string PaymentId,
    string PayhereAmount,
    string PayhereCurrency,
    string StatusCode,
    string Md5Sig,
    string? Method);

public sealed record PayHereWebhookProcessResult(
    bool Success,
    bool AlreadyProcessed,
    bool IsSubscriptionPayment,
    string Message);

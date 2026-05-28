namespace MyWedding.SharedKernel.Interfaces;

/// <summary>
/// Payment gateway abstraction for booking deposits and platform commission splits (PayHere in production).
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Charges the customer and splits proceeds between vendor net payout and platform take rate.
    /// </summary>
    Task<SplitPaymentResult> ProcessSplitPaymentAsync(
        SplitPaymentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SplitPaymentRequest(
    Guid BookingId,
    Guid VendorId,
    decimal GrossAmountLkr,
    decimal PlatformTakeRate = 0.05m,
    string Currency = "LKR",
    string? IdempotencyKey = null);

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
    bool IsSimulated);

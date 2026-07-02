using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services.Mocks;

/// <summary>
/// Simulates PayHere split settlement until live gateway integration is configured.
/// </summary>
public class MockPaymentGatewayService : IPaymentGatewayService
{
    public async Task<SplitPaymentResult> ProcessSplitPaymentAsync(
        SplitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(80, cancellationToken);

        if (request.GrossAmountLkr <= 0)
        {
            return new SplitPaymentResult(
                Success: false,
                GatewayName: "MockPayHere",
                GatewayPaymentId: string.Empty,
                GrossAmountLkr: request.GrossAmountLkr,
                PlatformCommissionLkr: 0,
                VendorNetPayoutLkr: 0,
                PlatformTakeRateApplied: request.PlatformTakeRate,
                Status: "failed",
                Message: "Gross amount must be greater than zero.",
                IsSimulated: true);
        }

        var takeRate = request.PlatformTakeRate is > 0 and <= 1 ? request.PlatformTakeRate : 0.05m;
        var commission = Math.Round(request.GrossAmountLkr * takeRate, 2, MidpointRounding.AwayFromZero);
        var vendorNet = request.GrossAmountLkr - commission;
        var paymentId = $"mock_ph_{request.BookingId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        return new SplitPaymentResult(
            Success: true,
            GatewayName: "MockPayHere",
            GatewayPaymentId: paymentId,
            GrossAmountLkr: request.GrossAmountLkr,
            PlatformCommissionLkr: commission,
            VendorNetPayoutLkr: vendorNet,
            PlatformTakeRateApplied: takeRate,
            Status: "captured",
            Message:
                $"Simulated split payment: vendor receives LKR {vendorNet:N2}, platform take rate LKR {commission:N2} ({takeRate:P0}).",
            IsSimulated: true);
    }

    public Task<PayHereWebhookProcessResult> ProcessPayHereWebhookAsync(
        PayHereWebhookNotification notification,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PayHereWebhookProcessResult(
            true,
            false,
            false,
            "Mock gateway does not process webhooks."));
}

namespace MyWedding.SharedKernel.Interfaces;

public interface IPaymentWorkflowService
{
    Task<PaymentWorkflowResult> CreateDepositCheckoutAsync(
        string? userId,
        string? userEmail,
        Guid bookingId,
        string requestScheme,
        string requestHost,
        CancellationToken cancellationToken = default);

    Task<PaymentWorkflowResult> CreatePlannerSubscriptionCheckoutAsync(
        string? plannerId,
        string? userEmail,
        int tier,
        decimal monthlyFee,
        string requestScheme,
        string requestHost,
        CancellationToken cancellationToken = default);

    Task<PaymentWorkflowResult> HandlePayHereWebhookAsync(
        PayHereWebhookRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentWorkflowResult> GetBookingPaymentStatusAsync(
        string? userId,
        bool isAdmin,
        Guid bookingId,
        CancellationToken cancellationToken = default);
}

public record PaymentWorkflowResult(int StatusCode, object? Body);

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

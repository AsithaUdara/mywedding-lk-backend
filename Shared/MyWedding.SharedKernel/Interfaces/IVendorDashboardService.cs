namespace MyWedding.SharedKernel.Interfaces;

public interface IVendorDashboardService
{
    Task<string?> GetVendorVerificationStatusAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> GetBusinessProfileAsync(
        string? userId,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> UpdateBusinessProfileAsync(
        string? userId,
        UpdateVendorBusinessProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> GetSubscriptionAsync(
        string? userId,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> SetSubscriptionAsync(
        string? userId,
        int tier,
        decimal monthlyFee,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> GetBillingProfileAsync(
        string? userId,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> SaveBillingProfileAsync(
        string? userId,
        VendorBillingProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<VendorDashboardWorkflowResult> CreateSubscriptionCheckoutAsync(
        string? userId,
        string? userEmail,
        int tier,
        decimal monthlyFee,
        string requestScheme,
        string requestHost,
        CancellationToken cancellationToken = default);
}

public record VendorDashboardWorkflowResult(int StatusCode, object? Body);

public record VendorBillingProfileRequest(
    string? CardholderName,
    string? CardBrand,
    string Last4,
    byte? ExpiryMonth,
    short? ExpiryYear);

public record UpdateVendorBusinessProfileRequest(
    string BusinessName,
    string? BusinessDescription,
    string? WebsiteUrl,
    string? ContactPhone,
    string City,
    string? Province);

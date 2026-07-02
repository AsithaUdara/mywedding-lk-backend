using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Payments;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class VendorDashboardService : IVendorDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public VendorDashboardService(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<string?> GetVendorVerificationStatusAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var status = await _db.Vendors
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => (VerificationStatus?)v.VerificationStatus)
            .FirstOrDefaultAsync(cancellationToken);

        return status?.ToString();
    }

    public async Task<VendorDashboardWorkflowResult> GetBusinessProfileAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        var vendor = await _db.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

        if (vendor is null)
            return new VendorDashboardWorkflowResult(404, new { message = "Vendor profile not found." });

        return new VendorDashboardWorkflowResult(200, new
        {
            userId = vendor.UserId,
            businessName = vendor.BusinessName,
            businessDescription = vendor.BusinessDescription,
            websiteUrl = vendor.WebsiteUrl,
            contactPhone = vendor.ContactPhone,
            city = vendor.City,
            province = vendor.Province,
            verificationStatus = vendor.VerificationStatus.ToString(),
        });
    }

    public async Task<VendorDashboardWorkflowResult> UpdateBusinessProfileAsync(
        string? userId,
        UpdateVendorBusinessProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        if (string.IsNullOrWhiteSpace(request.BusinessName))
            return new VendorDashboardWorkflowResult(400, new { message = "Business name is required." });

        if (string.IsNullOrWhiteSpace(request.City))
            return new VendorDashboardWorkflowResult(400, new { message = "City is required for your listing location." });

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);
        if (vendor is null)
            return new VendorDashboardWorkflowResult(404, new { message = "Vendor profile not found." });

        vendor.BusinessName = request.BusinessName.Trim();
        vendor.BusinessDescription = string.IsNullOrWhiteSpace(request.BusinessDescription)
            ? null
            : request.BusinessDescription.Trim();
        vendor.WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim();
        vendor.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        vendor.City = request.City.Trim();
        vendor.Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return new VendorDashboardWorkflowResult(200, new { message = "Business profile updated." });
    }

    public async Task<VendorDashboardWorkflowResult> GetSubscriptionAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        var sub = await _db.VendorSubscriptions
            .AsNoTracking()
            .Where(s => s.VendorId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub is null)
        {
            return new VendorDashboardWorkflowResult(200, new
            {
                tier = SubscriptionPlanTier.Free.ToString(),
                monthlyFee = 0m,
                status = SubscriptionStatus.Active.ToString(),
            });
        }

        return new VendorDashboardWorkflowResult(200, new
        {
            tier = sub.Tier.ToString(),
            monthlyFee = sub.MonthlyFee,
            status = sub.Status.ToString(),
        });
    }

    public async Task<VendorDashboardWorkflowResult> SetSubscriptionAsync(
        string? userId,
        int tier,
        decimal monthlyFee,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        var subscriptionTier = (SubscriptionPlanTier)tier;

        var existing = await _db.VendorSubscriptions
            .Where(s => s.VendorId == userId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var sub in existing)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.EndsAt = DateTime.UtcNow;
        }

        await _db.VendorSubscriptions.AddAsync(new VendorSubscription
        {
            Id = Guid.NewGuid(),
            VendorId = userId,
            Tier = subscriptionTier,
            Status = SubscriptionStatus.Active,
            MonthlyFee = monthlyFee,
            StartsAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return new VendorDashboardWorkflowResult(200, new
        {
            message = "Vendor subscription updated.",
            tier = subscriptionTier.ToString(),
        });
    }

    public async Task<VendorDashboardWorkflowResult> GetBillingProfileAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        var profile = await _db.VendorBillingProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.VendorId == userId, cancellationToken);

        if (profile is null)
            return new VendorDashboardWorkflowResult(200, new { hasPaymentMethod = false });

        return new VendorDashboardWorkflowResult(200, new
        {
            hasPaymentMethod = !string.IsNullOrEmpty(profile.Last4),
            cardholderName = profile.CardholderName,
            cardBrand = profile.CardBrand,
            last4 = profile.Last4,
            expiryMonth = profile.ExpiryMonth,
            expiryYear = profile.ExpiryYear,
        });
    }

    public async Task<VendorDashboardWorkflowResult> SaveBillingProfileAsync(
        string? userId,
        VendorBillingProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        if (string.IsNullOrWhiteSpace(request.Last4) || request.Last4.Length != 4 || !request.Last4.All(char.IsDigit))
        {
            return new VendorDashboardWorkflowResult(400, new
            {
                message = "Only the last 4 digits are stored. Enter a valid last-4.",
            });
        }

        var profile = await _db.VendorBillingProfiles.FirstOrDefaultAsync(p => p.VendorId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new VendorBillingProfile { VendorId = userId };
            await _db.VendorBillingProfiles.AddAsync(profile, cancellationToken);
        }

        profile.CardholderName = request.CardholderName?.Trim();
        profile.CardBrand = request.CardBrand?.Trim();
        profile.Last4 = request.Last4;
        profile.ExpiryMonth = request.ExpiryMonth;
        profile.ExpiryYear = request.ExpiryYear;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return new VendorDashboardWorkflowResult(200, new
        {
            message = "Payment method saved (masked). Full card numbers are never stored.",
        });
    }

    public async Task<VendorDashboardWorkflowResult> CreateSubscriptionCheckoutAsync(
        string? userId,
        string? userEmail,
        int tier,
        decimal monthlyFee,
        string requestScheme,
        string requestHost,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new VendorDashboardWorkflowResult(401, null);

        var subscriptionTier = (SubscriptionPlanTier)tier;

        if (subscriptionTier == SubscriptionPlanTier.Free || monthlyFee <= 0)
        {
            return new VendorDashboardWorkflowResult(400, new
            {
                message = "Checkout is only required for paid plans.",
            });
        }

        var checkout = new VendorSubscriptionCheckout
        {
            Id = Guid.NewGuid(),
            VendorId = userId,
            Tier = subscriptionTier,
            Amount = monthlyFee,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };
        await _db.VendorSubscriptionCheckouts.AddAsync(checkout, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var sandboxUrl = _configuration["PayHere:SandboxCheckoutUrl"] ?? "https://sandbox.payhere.lk/pay/checkout";
        var merchantId = _configuration["PayHere:MerchantId"] ?? "TEST_MERCHANT";
        var notifyUrl = _configuration["PayHere:NotifyUrl"]
            ?? $"{requestScheme}://{requestHost}/api/payments/payhere/webhook";
        var returnUrl = _configuration["PayHere:VendorSubscriptionReturnUrl"]
            ?? $"{_configuration["Frontend:BaseUrl"]}/vendor/dashboard/settings?payment=success";
        var cancelUrl = _configuration["PayHere:VendorSubscriptionCancelUrl"]
            ?? $"{_configuration["Frontend:BaseUrl"]}/vendor/dashboard/settings?payment=cancelled";

        var orderId = checkout.Id.ToString();
        var currency = "LKR";
        var merchantSecret = _configuration["PayHere:MerchantSecret"] ?? string.Empty;
        var email = userEmail ?? "vendor@mywedding.lk";
        var checkoutPayload = PayHereCheckoutFormBuilder.Build(
            sandboxUrl,
            merchantId,
            merchantSecret,
            orderId,
            monthlyFee,
            currency,
            $"Vendor {subscriptionTier} Plan",
            returnUrl,
            cancelUrl,
            notifyUrl,
            email,
            firstName: "Vendor",
            lastName: "Subscription");

        return new VendorDashboardWorkflowResult(200, new
        {
            checkoutId = checkout.Id,
            checkout = checkoutPayload,
        });
    }
}

public record VendorSelfSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);

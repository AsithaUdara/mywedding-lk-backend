using System.Globalization;

namespace MyWedding.Infrastructure.Payments;

/// <summary>
/// Builds PayHere Checkout API POST payloads (all required fields per support.payhere.lk).
/// </summary>
public static class PayHereCheckoutFormBuilder
{
    public const string DefaultPhone = "0770000000";
    public const string DefaultAddress = "Colombo";
    public const string DefaultCity = "Colombo";
    public const string DefaultCountry = "Sri Lanka";

    public static Dictionary<string, object> Build(
        string checkoutUrl,
        string merchantId,
        string merchantSecret,
        string orderId,
        decimal amount,
        string currency,
        string items,
        string returnUrl,
        string cancelUrl,
        string notifyUrl,
        string email,
        string? firstName = null,
        string? lastName = null,
        string? phone = null,
        string? address = null,
        string? city = null,
        string? country = null)
    {
        var amountFormatted = amount.ToString("F2", CultureInfo.InvariantCulture);
        var (first, last) = SplitName(firstName, lastName, email);

        var checkout = new Dictionary<string, object>
        {
            ["checkoutUrl"] = checkoutUrl,
            ["merchant_id"] = merchantId,
            ["return_url"] = returnUrl,
            ["cancel_url"] = cancelUrl,
            ["notify_url"] = notifyUrl,
            ["order_id"] = orderId,
            ["items"] = items,
            ["amount"] = amountFormatted,
            ["currency"] = currency,
            ["first_name"] = first,
            ["last_name"] = last,
            ["email"] = email,
            ["phone"] = string.IsNullOrWhiteSpace(phone) ? DefaultPhone : phone.Trim(),
            ["address"] = string.IsNullOrWhiteSpace(address) ? DefaultAddress : address.Trim(),
            ["city"] = string.IsNullOrWhiteSpace(city) ? DefaultCity : city.Trim(),
            ["country"] = string.IsNullOrWhiteSpace(country) ? DefaultCountry : country.Trim(),
        };

        if (!string.IsNullOrWhiteSpace(merchantSecret))
        {
            checkout["hash"] = PayHereHashHelper.BuildCheckoutHash(
                merchantId, orderId, amount, currency, merchantSecret);
        }

        return checkout;
    }

    private static (string First, string Last) SplitName(string? firstName, string? lastName, string email)
    {
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            return (firstName.Trim(), lastName.Trim());

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            var parts = firstName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1
                ? (parts[0], parts[1])
                : (parts[0], "User");
        }

        var local = email.Split('@')[0];
        return (local, "User");
    }
}

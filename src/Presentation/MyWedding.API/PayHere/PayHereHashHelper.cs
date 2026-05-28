using System.Security.Cryptography;
using System.Text;

namespace MyWedding.API.PayHere;

/// <summary>
/// PayHere Checkout API hash generation (see support.payhere.lk checkout API).
/// </summary>
public static class PayHereHashHelper
{
    public static string BuildCheckoutHash(
        string merchantId,
        string orderId,
        decimal amount,
        string currency,
        string merchantSecret)
    {
        var amountFormatted = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var secretHash = Md5HexUpper(merchantSecret);
        var raw = $"{merchantId}{orderId}{amountFormatted}{currency}{secretHash}";
        return Md5HexUpper(raw);
    }

    public static string BuildWebhookSignature(
        string merchantId,
        string orderId,
        string payhereAmount,
        string payhereCurrency,
        string statusCode,
        string merchantSecret)
    {
        var secretHash = Md5HexUpper(merchantSecret);
        var raw = $"{merchantId}{orderId}{payhereAmount}{payhereCurrency}{statusCode}{secretHash}";
        return Md5HexUpper(raw);
    }

    private static string Md5HexUpper(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

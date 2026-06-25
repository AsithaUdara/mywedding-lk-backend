namespace MyWedding.Infrastructure.Payments;

public static class PayHereCardMetadataHelper
{
    public static string? ExtractLast4(string? maskedCardNo)
    {
        if (string.IsNullOrWhiteSpace(maskedCardNo))
            return null;

        var digits = new string(maskedCardNo.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : null;
    }

    public static (byte? Month, short? Year) ParseCardExpiry(string? cardExpiry)
    {
        if (string.IsNullOrWhiteSpace(cardExpiry))
            return (null, null);

        var clean = cardExpiry.Replace("/", "", StringComparison.Ordinal).Trim();
        if (clean.Length != 4)
            return (null, null);

        if (!byte.TryParse(clean[..2], out var month) || month is < 1 or > 12)
            return (null, null);

        if (!short.TryParse(clean[2..], out var yy))
            return (null, null);

        var year = yy >= 100 ? yy : (short)(2000 + yy);
        return (month, year);
    }
}

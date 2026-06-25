namespace MyWedding.SharedKernel;

/// <summary>
/// Resolves a human-readable name when profile fields are missing or Firebase placeholders.
/// </summary>
public static class UserDisplayNameHelper
{
    private static readonly HashSet<string> PlaceholderFirstNames =
        new(StringComparer.OrdinalIgnoreCase) { "User", "Unknown" };

    public static string GetDisplayName(string? firstName, string? lastName, string? email)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        var full = $"{first} {last}".Trim();

        if (!string.IsNullOrEmpty(full) &&
            !(PlaceholderFirstNames.Contains(first) && string.IsNullOrEmpty(last)))
        {
            return full;
        }

        return NameFromEmail(email ?? string.Empty);
    }

    public static string NameFromEmail(string email)
    {
        var localPart = (email.Split('@').FirstOrDefault() ?? email).Trim();
        if (string.IsNullOrEmpty(localPart))
            return "Client";

        return string.Join(
            ' ',
            localPart
                .Replace('_', ' ')
                .Replace('.', ' ')
                .Replace('-', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(static part =>
                    part.Length == 0
                        ? part
                        : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

}

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors
{
    public record VendorDto(
        string UserId,
        string BusinessName,
        string? BusinessDescription,
        string? WebsiteUrl,
        string? ContactPhone,
        string City,
        string VerificationStatus,
        decimal AverageRating,
        int TotalReviews,
        decimal MinPrice,
        string CategoryName,
        string? PrimaryImageUrl,
        IReadOnlyList<string> ImageUrls,
        string PremiumTier = "Free",
        bool IsSponsored = false,
        bool IsFeatured = false
    );
}

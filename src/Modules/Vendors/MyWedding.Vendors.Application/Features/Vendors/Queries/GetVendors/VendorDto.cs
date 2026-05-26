namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors
{
    public record VendorDto(
        string UserId, // This is the Firebase UID, which is our PK for Vendor
        string BusinessName,
        string? BusinessDescription,
        string? WebsiteUrl,
        string? ContactPhone,
        string City,
        string VerificationStatus, // Enum as string
        decimal AverageRating,
        int TotalReviews,
        decimal MinPrice,
        string CategoryName,
        string? PrimaryImageUrl,
        IReadOnlyList<string> ImageUrls
    );
}

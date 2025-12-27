namespace MyWedding.Application.Features.Vendors.Queries.GetVendors
{
    public record VendorDto(
        string UserId, // This is the Firebase UID, which is our PK for Vendor
        string BusinessName,
        string? BusinessDescription,
        string? WebsiteUrl,
        string City,
        string VerificationStatus, // Enum as string
        decimal AverageRating,
        string CategoryName // Added to show category directly
    );
}

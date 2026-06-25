
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorById
{
    public class VendorDetailDto
    {
        public required string UserId { get; set; }
        public required string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactPhone { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public decimal AverageRating { get; set; }
        public string? CoverImageUrl { get; set; }
        public IReadOnlyList<string> GalleryImageUrls { get; set; } = [];
        public IEnumerable<VendorServiceDto> Services { get; set; } = new List<VendorServiceDto>();
        public IEnumerable<VendorReviewDto> Reviews { get; set; } = new List<VendorReviewDto>();
    }

    public record VendorServiceDto(
        Guid Id,
        string ServiceName,
        string Description,
        decimal BasePrice,
        PricingType PricingType,
        string? PrimaryImageUrl,
        IReadOnlyList<string> GalleryUrls,
        string? Tagline,
        string? ListingDetailsJson);

    public record VendorReviewDto(Guid Id, string ReviewerName, int Rating, string ReviewContent, System.DateTime CreatedAt);
}

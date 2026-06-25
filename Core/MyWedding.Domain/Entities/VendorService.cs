using MyWedding.Domain.Enums;
using System;

namespace MyWedding.Domain.Entities
{
    public class VendorService
    {
        public Guid Id { get; set; }
        public required string ServiceName { get; set; }
        public string? ServiceDescription { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public bool IsActive { get; set; } = true;
        public string? PrimaryImageUrl { get; set; }
        public string? GalleryUrlsJson { get; set; }
        public string? Tagline { get; set; }
        public string? ListingDetailsJson { get; set; }

        // Foreign Key to Vendor
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        // Foreign Key to VendorCategory
        public Guid CategoryId { get; set; }
        public VendorCategory? Category { get; set; }
    }
}

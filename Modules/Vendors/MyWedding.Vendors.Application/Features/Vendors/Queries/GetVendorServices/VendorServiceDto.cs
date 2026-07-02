// File: src/Core/MyWedding.Application/Features/Vendors/Queries/GetVendorServices/VendorServiceDto.cs

using System;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorServices
{
    public class VendorServiceDto
    {
        public Guid Id { get; set; }
        public required string ServiceName { get; set; }
        public string? ServiceDescription { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public required string CategoryName { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public IReadOnlyList<string> GalleryUrls { get; set; } = [];
        public string? Tagline { get; set; }
        public string? ListingDetailsJson { get; set; }
    }
}

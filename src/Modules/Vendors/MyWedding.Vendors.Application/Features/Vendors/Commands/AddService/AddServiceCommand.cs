using MediatR;

using System;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.AddService
{
    public class AddServiceCommand : IRequest<Guid>
    {
        public string? VendorId { get; set; }
        public required string ServiceName { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? PrimaryImageUrl { get; set; }
        public List<string>? GalleryUrls { get; set; }
        public string? Tagline { get; set; }
        public string? ListingDetailsJson { get; set; }
    }
}

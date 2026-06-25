using MyWedding.Domain.Enums;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class Vendor
    {
        // The PK is also a FK to the Users table
        public required string UserId { get; set; }
        public User? User { get; set; }

        public required string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactPhone { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public decimal AverageRating { get; set; } // This is a derived/calculated field
        public string? CoverImageUrl { get; set; }
        public string? GalleryUrlsJson { get; set; }

        // New: Primary Service Category
        public Guid? PrimaryCategoryId { get; set; }
        public VendorCategory? PrimaryCategory { get; set; }

        // Navigation Properties
        public ICollection<VendorService> Services { get; set; } = new List<VendorService>();
        public ICollection<VendorReview> Reviews { get; set; } = new List<VendorReview>();
        public ICollection<VendorBooking> Bookings { get; set; } = new List<VendorBooking>();
        public ICollection<VendorInquiry> Inquiries { get; set; } = new List<VendorInquiry>();
    }
}

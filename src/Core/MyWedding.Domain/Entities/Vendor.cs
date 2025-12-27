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
        public string? City { get; set; }
        public string? Province { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public decimal AverageRating { get; set; } // This is a derived/calculated field

        // Navigation Properties
        public ICollection<VendorService> Services { get; set; } = new List<VendorService>();
        public ICollection<VendorReview> Reviews { get; set; } = new List<VendorReview>();
    }
}

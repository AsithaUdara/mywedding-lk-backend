using System;

namespace MyWedding.Domain.Entities
{
    public class VendorReview
    {
        public Guid Id { get; set; }
        public int Rating { get; set; } // e.g., 1 to 5
        public string? ReviewContent { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign Key to the Vendor being reviewed
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        // Foreign Key to the User who wrote the review
        public required string ReviewerId { get; set; }
        public User? Reviewer { get; set; }

        // Foreign Key to the WeddingEvent this review is about
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }
    }
}

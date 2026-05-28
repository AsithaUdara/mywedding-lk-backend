using System;

namespace MyWedding.Domain.Entities
{
    public class VendorInquiry
    {
        public Guid Id { get; set; }
        public required string Message { get; set; }
        public string? Subject { get; set; }
        public required string SenderEmail { get; set; }
        public required string SenderId { get; set; }
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public Guid? EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}

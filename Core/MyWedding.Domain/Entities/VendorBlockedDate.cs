using System;

namespace MyWedding.Domain.Entities
{
    public class VendorBlockedDate
    {
        public Guid Id { get; set; }
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System;

namespace MyWedding.Domain.Entities
{
    public class VendorProfileView
    {
        public Guid Id { get; set; }
        public required string VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}

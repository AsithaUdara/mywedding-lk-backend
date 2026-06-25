using System;

namespace MyWedding.Domain.Entities
{
    public class BookingContract
    {
        // The Primary Key is also the Foreign Key to VendorBooking (1-to-1 relationship)
        public Guid Id { get; set; }
        public VendorBooking? VendorBooking { get; set; }

        public string? ContractFileUrl { get; set; }
        public byte[]? PdfContent { get; set; }
        public DateTime? ClientSignedAt { get; set; }
        public DateTime? VendorSignedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

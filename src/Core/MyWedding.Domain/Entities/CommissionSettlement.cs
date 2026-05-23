using System;

namespace MyWedding.Domain.Entities
{
    public class CommissionSettlement
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public VendorBooking? Booking { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal VendorNetAmount { get; set; }
        public decimal CommissionRate { get; set; } = 0.05m;

        public bool IsVendorPayoutSettled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}

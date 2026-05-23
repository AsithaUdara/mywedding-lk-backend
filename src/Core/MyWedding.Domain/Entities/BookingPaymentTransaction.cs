using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class BookingPaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public VendorBooking? Booking { get; set; }

        public required string GatewayName { get; set; } // e.g. PayHere
        public string? GatewayPaymentId { get; set; }
        public required string IdempotencyKey { get; set; }
        public decimal Amount { get; set; }
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
        public string? Currency { get; set; } = "LKR";
        public string? RawCallbackPayload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

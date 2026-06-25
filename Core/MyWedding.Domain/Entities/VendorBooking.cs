using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class VendorBooking
    {
        public Guid Id { get; set; }
        public BookingStatus Status { get; set; }
        public decimal FinalAmount { get; set; } // The agreed-upon price for the service
        public DateTime ServiceDate { get; set; }

        // Foreign Key to the WeddingEvent
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        // Foreign Key to the specific VendorService being booked
        public Guid ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        // Foreign Key to the User who made the booking
        public required string BookedById { get; set; }
        public User? BookedBy { get; set; }
        
        // One-to-one navigation to booking contract
        public BookingContract? BookingContract { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}

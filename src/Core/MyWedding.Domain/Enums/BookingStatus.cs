namespace MyWedding.Domain.Enums
{
    public enum BookingStatus
    {
        Requested = 0,      // Booking requested by couple/planner
        Pending = 0,        // Legacy alias kept for backward compatibility
        AwaitingPayment = 1,
        Confirmed = 2,      // The booking is confirmed after payment or agreement
        Completed = 3,      // The event date has passed and the service was rendered
        Cancelled = 4       // The booking was cancelled by either party
    }
}

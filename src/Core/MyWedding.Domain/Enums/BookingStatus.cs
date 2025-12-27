namespace MyWedding.Domain.Enums
{
    public enum BookingStatus
    {
        Pending,        // The initial state after a request is made
        Confirmed,      // The booking is confirmed after payment or agreement
        Completed,      // The event date has passed and the service was rendered
        Cancelled       // The booking was cancelled by either party
    }
}

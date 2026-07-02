namespace MyWedding.Domain.Enums;

public enum VendorShortlistItemStatus
{
    Draft = 0,
    SentToClient = 1,
    ClientApproved = 2,
    ClientRejected = 3,
    BookingRequested = 4,
    BookingAccepted = 5,
    /// <summary>Vendor declined the booking request.</summary>
    Declined = 6,
    /// <summary>Client deposit paid; vendor booking confirmed.</summary>
    DepositPaid = 7,
    /// <summary>Client signed the vendor contract; deposit payment unlocked.</summary>
    ContractSigned = 8
}

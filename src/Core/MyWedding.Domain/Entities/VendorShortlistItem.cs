using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities;

public class VendorShortlistItem
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public WeddingEvent? WeddingEvent { get; set; }
    public Guid VendorServiceId { get; set; }
    public VendorService? VendorService { get; set; }
    public required string PlannerId { get; set; }
    public string? CategoryLabel { get; set; }
    public string? PlannerNotes { get; set; }
    public VendorShortlistItemStatus Status { get; set; }
    public decimal ProposedAmount { get; set; }
    public DateTime? ServiceDate { get; set; }
    public Guid? VendorBookingId { get; set; }
    public VendorBooking? VendorBooking { get; set; }
    public string? ClientApprovedByUserId { get; set; }
    public DateTime? ClientApprovedAt { get; set; }
    public DateTime? SentToClientAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

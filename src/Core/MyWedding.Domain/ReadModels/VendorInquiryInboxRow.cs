using System;

namespace MyWedding.Domain.ReadModels
{
    public class VendorInquiryInboxRow
    {
        public Guid Id { get; init; }
        public required string Message { get; init; }
        public string? Subject { get; init; }
        public required string SenderEmail { get; init; }
        public required string SenderId { get; init; }
        public required string VendorId { get; init; }
        public DateTime SentAt { get; init; }
        public bool IsRead { get; init; }
        public required string From { get; init; }
        public required string SenderName { get; init; }
        public required string SenderOrg { get; init; }
        public string? EventName { get; init; }
        public DateTime? WeddingDate { get; init; }
        public string? BudgetHint { get; init; }
    }
}

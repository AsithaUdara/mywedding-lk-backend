using System;

namespace MyWedding.Domain.Entities
{
    public class VendorInquiryQuote
    {
        public Guid Id { get; set; }
        public Guid InquiryId { get; set; }
        public VendorInquiry? Inquiry { get; set; }
        public required string VendorId { get; set; }
        public required string QuoteReference { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LKR";
        public required string Body { get; set; }
        public string? PdfStorageKey { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

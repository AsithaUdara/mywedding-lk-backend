using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.GenerateInquiryQuote
{
    public class GenerateInquiryQuoteCommand : IRequest<InquiryQuoteResultDto>
    {
        public Guid InquiryId { get; init; }
        public required string VendorId { get; init; }
        public decimal? ProposedAmount { get; init; }
    }

    public class InquiryQuoteResultDto
    {
        public Guid QuoteId { get; init; }
        public required string QuoteReference { get; init; }
        public decimal Amount { get; init; }
        public required string Currency { get; init; }
        public required string SuggestedReply { get; init; }
        public string? PdfStorageKey { get; init; }
    }
}

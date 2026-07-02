using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.GenerateInquiryQuote
{
    public class GenerateInquiryQuoteCommandValidator : AbstractValidator<GenerateInquiryQuoteCommand>
    {
        public GenerateInquiryQuoteCommandValidator()
        {
            RuleFor(v => v.InquiryId).NotEmpty();
            RuleFor(v => v.VendorId).ValidUserId();
            RuleFor(v => v.ProposedAmount).GreaterThan(0).When(v => v.ProposedAmount.HasValue);
        }
    }
}

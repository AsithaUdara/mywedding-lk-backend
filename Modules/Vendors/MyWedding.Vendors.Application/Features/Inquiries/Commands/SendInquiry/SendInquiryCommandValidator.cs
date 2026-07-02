using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.SendInquiry
{
    public class SendInquiryCommandValidator : AbstractValidator<SendInquiryCommand>
    {
        public SendInquiryCommandValidator()
        {
            RuleFor(v => v.VendorId).ValidUserId();
            RuleFor(v => v.SenderId).ValidUserId();
            RuleFor(v => v.SenderEmail).NotEmpty().EmailAddress();
            RuleFor(v => v.Message).NotEmpty().MaximumLength(4000);
        }
    }
}

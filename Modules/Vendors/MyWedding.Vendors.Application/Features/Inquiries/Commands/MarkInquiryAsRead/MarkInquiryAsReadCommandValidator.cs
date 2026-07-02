using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.MarkInquiryAsRead
{
    public class MarkInquiryAsReadCommandValidator : AbstractValidator<MarkInquiryAsReadCommand>
    {
        public MarkInquiryAsReadCommandValidator()
        {
            RuleFor(v => v.InquiryId).NotEmpty();
            RuleFor(v => v.VendorId).ValidUserId();
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.RegisterVendor
{
    public class RegisterVendorCommandValidator : AbstractValidator<RegisterVendorCommand>
    {
        public RegisterVendorCommandValidator()
        {
            RuleFor(v => v.UserId).ValidUserId();
            RuleFor(v => v.BusinessName).NotEmpty().MaximumLength(150);
            RuleFor(v => v.ContactPhone).MaximumLength(30).When(v => !string.IsNullOrWhiteSpace(v.ContactPhone));
            RuleFor(v => v.City).NotEmpty().MaximumLength(100);
        }
    }
}

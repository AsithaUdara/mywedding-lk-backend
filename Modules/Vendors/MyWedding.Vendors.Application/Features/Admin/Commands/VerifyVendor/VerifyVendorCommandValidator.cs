using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor
{
    public class VerifyVendorCommandValidator : AbstractValidator<VerifyVendorCommand>
    {
        public VerifyVendorCommandValidator()
        {
            RuleFor(v => v.VendorId).ValidUserId();
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.UpdateService
{
    public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
    {
        public UpdateServiceCommandValidator()
        {
            RuleFor(v => v.Id).NotEmpty();
            RuleFor(v => v.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(v => v.BasePrice).GreaterThanOrEqualTo(0);
            RuleFor(v => v.CategoryId).NotEmpty();
            RuleFor(v => v.PricingType).IsInEnum();
            RuleFor(v => v.Description).MaximumLength(2000).When(v => !string.IsNullOrWhiteSpace(v.Description));
            RuleFor(v => v.Tagline).MaximumLength(200).When(v => !string.IsNullOrWhiteSpace(v.Tagline));
        }
    }
}

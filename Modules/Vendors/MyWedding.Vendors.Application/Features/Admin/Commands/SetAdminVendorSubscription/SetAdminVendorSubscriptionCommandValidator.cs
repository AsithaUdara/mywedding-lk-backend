using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.SetAdminVendorSubscription;

public class SetAdminVendorSubscriptionCommandValidator : AbstractValidator<SetAdminVendorSubscriptionCommand>
{
    public SetAdminVendorSubscriptionCommandValidator()
    {
        RuleFor(v => v.VendorId).ValidUserId();
        RuleFor(v => v.Tier).IsInEnum();
        RuleFor(v => v.MonthlyFee).GreaterThanOrEqualTo(0);
    }
}

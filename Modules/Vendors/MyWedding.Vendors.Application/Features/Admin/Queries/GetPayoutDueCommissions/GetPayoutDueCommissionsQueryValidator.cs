using FluentValidation;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPayoutDueCommissions;

public class GetPayoutDueCommissionsQueryValidator : AbstractValidator<GetPayoutDueCommissionsQuery>
{
    public GetPayoutDueCommissionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search != null);
    }
}

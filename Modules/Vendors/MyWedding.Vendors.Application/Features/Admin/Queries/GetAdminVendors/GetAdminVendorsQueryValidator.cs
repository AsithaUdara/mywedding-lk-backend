using FluentValidation;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors;

public class GetAdminVendorsQueryValidator : AbstractValidator<GetAdminVendorsQuery>
{
    public GetAdminVendorsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search != null);
    }
}

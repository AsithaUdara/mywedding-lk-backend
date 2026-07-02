using FluentValidation;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

public class GetAdminEventAuditLogQueryValidator : AbstractValidator<GetAdminEventAuditLogQuery>
{
    public GetAdminEventAuditLogQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search != null);
        RuleFor(x => x.ActionType).MaximumLength(100).When(x => x.ActionType != null);
    }
}

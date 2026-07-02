using FluentValidation;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.MarkCommissionSettlementPaid;

public class MarkCommissionSettlementPaidCommandValidator : AbstractValidator<MarkCommissionSettlementPaidCommand>
{
    public MarkCommissionSettlementPaidCommandValidator()
    {
        RuleFor(v => v.SettlementId).NotEmpty();
    }
}

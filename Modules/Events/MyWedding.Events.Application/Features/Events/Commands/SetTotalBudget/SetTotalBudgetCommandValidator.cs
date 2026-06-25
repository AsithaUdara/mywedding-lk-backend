using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommandValidator : AbstractValidator<SetTotalBudgetCommand>
    {
        public SetTotalBudgetCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.TotalBudget).GreaterThan(0);
            RuleFor(v => v.UserId).ValidUserId().When(v => v.UserId is not null);
        }
    }
}

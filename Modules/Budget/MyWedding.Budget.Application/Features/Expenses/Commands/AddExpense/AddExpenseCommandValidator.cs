using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Budget.Application.Features.Expenses.Commands.AddExpense
{
    public class AddExpenseCommandValidator : AbstractValidator<AddExpenseCommand>
    {
        public AddExpenseCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.UserId).ValidUserId();
            RuleFor(v => v.Title).ValidTitle();
            RuleFor(v => v.Amount).GreaterThan(0);
            RuleFor(v => v.BudgetCategoryId).NotEmpty();
        }
    }
}

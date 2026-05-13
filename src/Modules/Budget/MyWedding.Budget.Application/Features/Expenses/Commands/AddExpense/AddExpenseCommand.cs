using MediatR;
using System;
using MyWedding.SharedKernel.Security;

namespace MyWedding.Budget.Application.Features.Expenses.Commands.AddExpense
{
    [Authorize]
    public class AddExpenseCommand : IRequest<Guid>, IAuthorizeableRequest
    {
        public Guid EventId { get; init; }
        public required string Title { get; init; }
        public decimal Amount { get; init; }
        public DateTime ExpenseDate { get; init; }
        public Guid BudgetCategoryId { get; init; }
        public required string UserId { get; init; }
    }
}

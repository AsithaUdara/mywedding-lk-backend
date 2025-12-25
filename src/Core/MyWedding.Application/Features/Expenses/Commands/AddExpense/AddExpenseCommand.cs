// File: src/Core/MyWedding.Application/Features/Expenses/Commands/AddExpense/AddExpenseCommand.cs
using MediatR;
using System;

namespace MyWedding.Application.Features.Expenses.Commands.AddExpense
{
    public class AddExpenseCommand : IRequest<Guid>
    {
        public Guid EventId { get; init; }
        public required string Title { get; init; }
        public decimal Amount { get; init; }
        public DateTime ExpenseDate { get; init; }
        public Guid BudgetCategoryId { get; init; } // Link to the category
    }
}

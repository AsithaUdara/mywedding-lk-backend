// File: src/Core/MyWedding.Domain/Entities/Expense.cs

using System;

namespace MyWedding.Domain.Entities
{
    public class Expense
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public decimal Amount { get; set; } // Use 'decimal' for financial values - this is a best practice
        public DateTime ExpenseDate { get; set; }

        // Foreign Key to the WeddingEvent this expense belongs to
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        // Foreign Key to the BudgetCategory
        public Guid BudgetCategoryId { get; set; }
        public BudgetCategory? BudgetCategory { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

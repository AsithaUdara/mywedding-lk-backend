namespace MyWedding.API.Controllers.Requests;

/// <summary>Request DTO for adding a new expense to an event's budget.</summary>
public record AddExpenseRequest(string Title, decimal Amount, DateTime ExpenseDate, Guid BudgetCategoryId);

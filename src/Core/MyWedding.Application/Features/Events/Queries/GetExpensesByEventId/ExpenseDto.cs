namespace MyWedding.Application.Features.Events.Queries.GetExpensesByEventId;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid EventId { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public string? BudgetCategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
}

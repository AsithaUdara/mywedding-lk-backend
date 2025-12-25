using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Application.Features.Events.Queries.GetExpensesByEventId;

public class GetExpensesByEventIdQueryHandler : IRequestHandler<GetExpensesByEventIdQuery, IEnumerable<ExpenseDto>>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public GetExpensesByEventIdQueryHandler(
        IExpenseRepository expenseRepository,
        IBudgetCategoryRepository budgetCategoryRepository)
    {
        _expenseRepository = expenseRepository;
        _budgetCategoryRepository = budgetCategoryRepository;
    }

    public async Task<IEnumerable<ExpenseDto>> Handle(GetExpensesByEventIdQuery request, CancellationToken cancellationToken)
    {
        var expenses = await _expenseRepository.GetByEventIdAsync(request.EventId, cancellationToken);
        var categories = await _budgetCategoryRepository.GetAllAsync(cancellationToken);
        
        var categoryDictionary = categories.ToDictionary(c => c.Id, c => c.Name);

        return expenses.Select(expense => new ExpenseDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            EventId = expense.EventId,
            BudgetCategoryId = expense.BudgetCategoryId,
            BudgetCategoryName = categoryDictionary.GetValueOrDefault(expense.BudgetCategoryId),
            CreatedAt = expense.CreatedAt
        });
    }
}

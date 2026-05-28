using MediatR;


namespace MyWedding.Events.Application.Features.Events.Queries.GetExpensesByEventId;

public class GetExpensesByEventIdQueryHandler : IRequestHandler<GetExpensesByEventIdQuery, IEnumerable<ExpenseDto>>
{
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public GetExpensesByEventIdQueryHandler(
        IWeddingEventRepository eventRepository,
        IExpenseRepository expenseRepository,
        IBudgetCategoryRepository budgetCategoryRepository)
    {
        _eventRepository = eventRepository;
        _expenseRepository = expenseRepository;
        _budgetCategoryRepository = budgetCategoryRepository;
    }

    public async Task<IEnumerable<ExpenseDto>> Handle(GetExpensesByEventIdQuery request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new ForbiddenAccessException("You do not have access to this event.");
        }

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

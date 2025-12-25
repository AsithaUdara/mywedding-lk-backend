// File: src/Core/MyWedding.Application/Features/Events/Queries/GetBudgetOverview/GetBudgetOverviewQueryHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Queries.GetBudgetOverview
{
    public class GetBudgetOverviewQueryHandler : IRequestHandler<GetBudgetOverviewQuery, BudgetOverviewDto?>
    {
        private readonly IWeddingEventRepository _eventRepository;
        private readonly IExpenseRepository _expenseRepository;

        public GetBudgetOverviewQueryHandler(
            IWeddingEventRepository eventRepository,
            IExpenseRepository expenseRepository)
        {
            _eventRepository = eventRepository;
            _expenseRepository = expenseRepository;
        }

        public async Task<BudgetOverviewDto?> Handle(GetBudgetOverviewQuery request, CancellationToken cancellationToken)
        {
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                return null; // Or throw NotFoundException if that's preferred
            }

            var expenses = await _expenseRepository.GetByEventIdAsync(request.EventId, cancellationToken);
            var totalSpent = expenses.Sum(e => e.Amount);
            var remainingBudget = weddingEvent.TotalBudget - totalSpent;

            return new BudgetOverviewDto(
                request.EventId,
                weddingEvent.TotalBudget,
                totalSpent,
                remainingBudget
            );
        }
    }
}

using MediatR;

namespace MyWedding.Application.Features.Events.Queries.GetExpensesByEventId;

public record GetExpensesByEventIdQuery(Guid EventId) : IRequest<IEnumerable<ExpenseDto>>;

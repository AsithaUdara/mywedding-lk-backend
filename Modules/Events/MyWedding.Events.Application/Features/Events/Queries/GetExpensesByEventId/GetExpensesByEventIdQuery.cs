using MediatR;

namespace MyWedding.Events.Application.Features.Events.Queries.GetExpensesByEventId;

public record GetExpensesByEventIdQuery(Guid EventId) : IRequest<IEnumerable<ExpenseDto>>;

using MediatR;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.API.Features.Planner;

public class CreatePlannerEventCommand : IRequest<CreatePlannerEventResult>, IPlannerSubscriptionLimitedRequest
{
    public required string PlannerId { get; init; }
    public required string EventName { get; init; }
    public DateTime EventDate { get; init; }
    public decimal TotalBudget { get; init; }
    public string? ClientUserId { get; init; }
    public string? ClientEmail { get; init; }
}

public record CreatePlannerEventResult(Guid EventId, Guid PlannerClientEventId, int TasksGenerated);

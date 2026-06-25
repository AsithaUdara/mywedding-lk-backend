using MediatR;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class ApplyPlannerTaskTemplateCommand : IRequest<ApplyPlannerTaskTemplateResult>
{
    public required string PlannerId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid TemplateId { get; init; }
    public bool ReplaceExisting { get; init; }
}

public record ApplyPlannerTaskTemplateResult(int TasksCreated, string TaskPlanPhase);

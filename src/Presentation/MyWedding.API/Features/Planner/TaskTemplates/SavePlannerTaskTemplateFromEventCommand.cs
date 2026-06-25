using MediatR;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class SavePlannerTaskTemplateFromEventCommand : IRequest<SavePlannerTaskTemplateResult>
{
    public required string PlannerId { get; init; }
    public required Guid EventId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public record SavePlannerTaskTemplateResult(Guid TemplateId, int TaskCount);

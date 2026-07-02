using MediatR;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class DeletePlannerTaskTemplateCommand : IRequest
{
    public required string PlannerId { get; init; }
    public required Guid TemplateId { get; init; }
}

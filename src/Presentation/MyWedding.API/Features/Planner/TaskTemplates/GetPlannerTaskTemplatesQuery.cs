using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class GetPlannerTaskTemplatesQuery : IRequest<IReadOnlyList<PlannerTaskTemplateListItemDto>>
{
    public required string PlannerId { get; init; }
}

public record PlannerTaskTemplateListItemDto(
    Guid Id,
    string Name,
    string? Description,
    PlannerTaskTemplateScheduleMode ScheduleMode,
    int TaskCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

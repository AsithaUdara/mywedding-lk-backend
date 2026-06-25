using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class GetPlannerTaskTemplatesQueryHandler
    : IRequestHandler<GetPlannerTaskTemplatesQuery, IReadOnlyList<PlannerTaskTemplateListItemDto>>
{
    private readonly IPlannerTaskTemplateRepository _templateRepository;

    public GetPlannerTaskTemplatesQueryHandler(IPlannerTaskTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<IReadOnlyList<PlannerTaskTemplateListItemDto>> Handle(
        GetPlannerTaskTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var summaries = await _templateRepository.GetSummariesByPlannerIdAsync(
            request.PlannerId,
            cancellationToken);

        return summaries
            .Select(s => new PlannerTaskTemplateListItemDto(
                s.Id,
                s.Name,
                s.Description,
                s.ScheduleMode,
                s.TaskCount,
                s.CreatedAt,
                s.UpdatedAt))
            .ToList();
    }
}

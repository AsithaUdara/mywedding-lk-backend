using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class GetPlannerTaskTemplatesQueryHandler
    : IRequestHandler<GetPlannerTaskTemplatesQuery, IReadOnlyList<PlannerTaskTemplateListItemDto>>
{
    private readonly ApplicationDbContext _db;

    public GetPlannerTaskTemplatesQueryHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlannerTaskTemplateListItemDto>> Handle(
        GetPlannerTaskTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.PlannerTaskTemplates
            .AsNoTracking()
            .Where(t => t.PlannerId == request.PlannerId)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new PlannerTaskTemplateListItemDto(
                t.Id,
                t.Name,
                t.Description,
                t.ScheduleMode,
                t.Items.Count,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}

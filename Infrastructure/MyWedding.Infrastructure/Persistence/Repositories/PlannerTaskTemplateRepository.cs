using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class PlannerTaskTemplateRepository : IPlannerTaskTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public PlannerTaskTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> CountByPlannerIdAsync(string plannerId, CancellationToken cancellationToken = default) =>
        _context.PlannerTaskTemplates.CountAsync(t => t.PlannerId == plannerId, cancellationToken);

    public async Task<IReadOnlyList<PlannerTaskTemplateSummary>> GetSummariesByPlannerIdAsync(
        string plannerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PlannerTaskTemplates
            .AsNoTracking()
            .Where(t => t.PlannerId == plannerId)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new PlannerTaskTemplateSummary(
                t.Id,
                t.Name,
                t.Description,
                t.ScheduleMode,
                t.Items.Count,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<PlannerTaskTemplate?> GetByIdWithItemsAsync(
        Guid templateId,
        string plannerId,
        CancellationToken cancellationToken = default) =>
        _context.PlannerTaskTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(
                t => t.Id == templateId && t.PlannerId == plannerId,
                cancellationToken);

    public Task<PlannerTaskTemplate?> GetByIdAsync(
        Guid templateId,
        string plannerId,
        CancellationToken cancellationToken = default) =>
        _context.PlannerTaskTemplates
            .FirstOrDefaultAsync(
                t => t.Id == templateId && t.PlannerId == plannerId,
                cancellationToken);

    public Task AddAsync(PlannerTaskTemplate template, CancellationToken cancellationToken = default) =>
        _context.PlannerTaskTemplates.AddAsync(template, cancellationToken).AsTask();

    public void Remove(PlannerTaskTemplate template) =>
        _context.PlannerTaskTemplates.Remove(template);
}

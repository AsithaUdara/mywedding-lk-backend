using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Interfaces;

public record PlannerTaskTemplateSummary(
    Guid Id,
    string Name,
    string? Description,
    PlannerTaskTemplateScheduleMode ScheduleMode,
    int TaskCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public interface IPlannerTaskTemplateRepository
{
    Task<int> CountByPlannerIdAsync(string plannerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlannerTaskTemplateSummary>> GetSummariesByPlannerIdAsync(
        string plannerId,
        CancellationToken cancellationToken = default);

    Task<PlannerTaskTemplate?> GetByIdWithItemsAsync(
        Guid templateId,
        string plannerId,
        CancellationToken cancellationToken = default);

    Task<PlannerTaskTemplate?> GetByIdAsync(
        Guid templateId,
        string plannerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlannerTaskTemplate template, CancellationToken cancellationToken = default);

    void Remove(PlannerTaskTemplate template);
}

using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities;

public class PlannerTaskTemplate
{
    public Guid Id { get; set; }
    public required string PlannerId { get; set; }
    public WeddingPlanner? Planner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PlannerTaskTemplateScheduleMode ScheduleMode { get; set; } = PlannerTaskTemplateScheduleMode.DaysBeforeWedding;
    public Guid? SourceEventId { get; set; }
    public WeddingEvent? SourceEvent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<PlannerTaskTemplateItem> Items { get; set; } = new List<PlannerTaskTemplateItem>();
}

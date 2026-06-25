namespace MyWedding.Domain.Entities;

public class PlannerTaskTemplateItem
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public PlannerTaskTemplate? Template { get; set; }
    public int SortOrder { get; set; }
    public required string Title { get; set; }
    public int StartOffsetDays { get; set; }
    public int DueOffsetDays { get; set; }
    public int? DependsOnSortOrder { get; set; }
}

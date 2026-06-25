namespace MyWedding.Domain.Interfaces
{
    public interface ICurrentPlannerAccessor
    {
        string? PlannerId { get; }
        bool IsPlanner { get; }
    }
}

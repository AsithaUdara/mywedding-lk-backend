namespace MyWedding.Domain.Enums;

/// <summary>
/// Tracks whether an event has discovery-only tasks or the full wedding checklist on the Gantt.
/// </summary>
public enum TaskPlanPhase
{
    None = 0,
    Discovery = 1,
    Full = 2
}

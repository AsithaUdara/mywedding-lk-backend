namespace MyWedding.Domain.Enums;

/// <summary>
/// How tasks are seeded when a planner creates a new wedding event.
/// </summary>
public enum EventTaskSeedMode
{
    /// <summary>Empty timeline — planner builds tasks manually.</summary>
    Manual = 0,

    /// <summary>8 discovery tasks from today forward (lead / onboarding phase).</summary>
    DiscoveryStarter = 1,

    /// <summary>Full 50-task master checklist scheduled backward from wedding date.</summary>
    MasterChecklist = 2,

    /// <summary>Planner-owned reusable template (requires CustomTemplateId).</summary>
    CustomTemplate = 3
}

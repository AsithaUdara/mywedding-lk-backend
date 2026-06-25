namespace MyWedding.Domain.Enums;

/// <summary>
/// How template item offsets map to calendar dates when applied to an event.
/// </summary>
public enum PlannerTaskTemplateScheduleMode
{
    /// <summary>Offsets are days before the wedding date (master-checklist style).</summary>
    DaysBeforeWedding = 0,

    /// <summary>Offsets are days from plan start (discovery / relative style).</summary>
    DaysFromPlanStart = 1
}

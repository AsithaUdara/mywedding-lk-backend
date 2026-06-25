using MyWedding.Domain.Entities;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Templates;

/// <summary>
/// Wedding-anchored backward scheduling with proportional compression when the wedding is closer than the full template horizon.
/// </summary>
public static class WeddingTaskScheduleCalculator
{
    public const int FullPlanningHorizonDays = 365;
    public const int CompressedTimelineThresholdDays = 120;

    public readonly record struct ScheduleWindow(DateTime StartDate, DateTime DueDate);

    public static ScheduleWindow ComputeScheduleDates(
        DateTime weddingDate,
        DateTime planStart,
        int startDaysBeforeWedding,
        int dueDaysBeforeWedding)
    {
        var wedding = weddingDate.Date;
        var today = planStart.Date;
        var daysUntilWedding = Math.Max(1, (wedding - today).Days);

        if (startDaysBeforeWedding <= 0 && dueDaysBeforeWedding <= 0)
        {
            return new ScheduleWindow(wedding, wedding);
        }

        var scale = daysUntilWedding >= FullPlanningHorizonDays
            ? 1.0
            : daysUntilWedding / (double)FullPlanningHorizonDays;

        var scaledStartDays = Math.Max(0, (int)Math.Round(startDaysBeforeWedding * scale));
        var scaledDueDays = Math.Max(0, (int)Math.Round(dueDaysBeforeWedding * scale));

        if (scaledDueDays > scaledStartDays)
        {
            (scaledStartDays, scaledDueDays) = (scaledDueDays, scaledStartDays);
        }

        var durationDays = Math.Max(1, scaledStartDays - scaledDueDays);

        var due = wedding.AddDays(-scaledDueDays);
        var start = wedding.AddDays(-scaledStartDays);

        if (start < today)
        {
            start = today;
            due = start.AddDays(durationDays);
        }

        if (due < today)
        {
            due = today.AddDays(Math.Min(7, Math.Max(1, daysUntilWedding / 14)));
        }

        if (due > wedding)
        {
            due = wedding;
        }

        if (start > due)
        {
            start = due.AddDays(-Math.Min(durationDays, Math.Max(0, (due - today).Days)));
            if (start < today)
            {
                start = today;
            }
        }

        return new ScheduleWindow(start, due);
    }

    /// <summary>
    /// For weddings under ~4 months, skip early onboarding tasks that no longer apply.
    /// </summary>
    public static bool ShouldIncludeTemplateEntry(int dueDaysBeforeWedding, int daysUntilWedding)
    {
        if (daysUntilWedding >= CompressedTimelineThresholdDays)
        {
            return true;
        }

        var scale = daysUntilWedding / (double)FullPlanningHorizonDays;
        var scaledDueDays = (int)Math.Round(dueDaysBeforeWedding * scale);

        return scaledDueDays <= daysUntilWedding || dueDaysBeforeWedding <= 45;
    }

    public static void EnforceDependencyOrderByGraph(
        IList<EventTask> tasksToAdjust,
        DateTime weddingDate,
        IEnumerable<EventTask>? allTasksForLookup = null)
    {
        var wedding = weddingDate.Date;
        var byId = (allTasksForLookup ?? tasksToAdjust).ToDictionary(t => t.Id);

        for (var pass = 0; pass < tasksToAdjust.Count; pass++)
        {
            var changed = false;

            foreach (var task in tasksToAdjust)
            {
                if (task.Status == DomainTaskStatus.Completed ||
                    !task.DependsOnTaskId.HasValue ||
                    !byId.TryGetValue(task.DependsOnTaskId.Value, out var dependency) ||
                    !dependency.DueDate.HasValue)
                {
                    continue;
                }

                var minStart = dependency.DueDate.Value.Date.AddDays(1);
                if (!task.StartDate.HasValue || task.StartDate.Value.Date < minStart)
                {
                    var duration = task.StartDate.HasValue && task.DueDate.HasValue
                        ? Math.Max(1, (task.DueDate.Value.Date - task.StartDate.Value.Date).Days)
                        : 7;

                    task.StartDate = minStart;
                    task.DueDate = minStart.AddDays(duration);
                    changed = true;
                }

                if (task.DueDate.HasValue && task.DueDate.Value.Date > wedding)
                {
                    task.DueDate = wedding;
                }

                if (task.StartDate.HasValue && task.DueDate.HasValue && task.StartDate > task.DueDate)
                {
                    task.StartDate = task.DueDate.Value.Date;
                }
            }

            if (!changed)
            {
                break;
            }
        }
    }

    public static int? FindTemplateIndexByTitle(string title)
    {
        var entries = WeddingTaskTemplate.Entries;
        for (var i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].Title, title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }
}

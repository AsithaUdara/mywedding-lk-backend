using MyWedding.Domain.Entities;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Templates;

public sealed record TemplateTaskPlan(
    int TemplateIndex,
    string Title,
    DateTime StartDate,
    DateTime DueDate,
    int? DependsOnTemplateIndex);

public static class ChecklistTemplatePlanner
{
    public static IReadOnlyList<TemplateTaskPlan> BuildFullTemplatePreview(
        DateTime weddingDate,
        DateTime planStartUtc)
    {
        var wedding = weddingDate.Date;
        var now = planStartUtc.Date;
        var daysUntilWedding = Math.Max(1, (wedding - now).Days);
        var template = WeddingTaskTemplate.Entries;

        var includedIndices = new List<int>();
        for (var i = 0; i < template.Count; i++)
        {
            if (WeddingTaskScheduleCalculator.ShouldIncludeTemplateEntry(template[i].DueDaysBeforeWedding, daysUntilWedding))
            {
                includedIndices.Add(i);
            }
        }

        if (!includedIndices.Contains(template.Count - 1))
        {
            includedIndices.Add(template.Count - 1);
            includedIndices.Sort();
        }

        return includedIndices
            .Select(index =>
            {
                var entry = template[index];
                var window = WeddingTaskScheduleCalculator.ComputeScheduleDates(
                    wedding,
                    planStartUtc,
                    entry.StartDaysBeforeWedding,
                    entry.DueDaysBeforeWedding);

                return new TemplateTaskPlan(
                    index,
                    entry.Title,
                    window.StartDate,
                    window.DueDate,
                    entry.DependsOnTemplateIndex);
            })
            .ToList();
    }

    public static HashSet<string> DiscoveryTaskTitles { get; } =
        WeddingDiscoveryTaskTemplate.Entries.Select(e => e.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsDiscoveryTaskTitle(string title) =>
        DiscoveryTaskTitles.Contains(title);
}

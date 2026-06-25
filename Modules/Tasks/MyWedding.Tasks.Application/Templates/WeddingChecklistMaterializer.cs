using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Templates;

public sealed record AdditionalChecklistTask(
    string Title,
    string? Description,
    DateTime? StartDate,
    DateTime? DueDate);

public static class WeddingChecklistMaterializer
{
    public static async Task<int> MaterializeFullChecklistAsync(
        Guid eventId,
        DateTime weddingDate,
        IEventTaskRepository taskRepository,
        IReadOnlySet<string>? excludeTitles,
        IReadOnlyList<AdditionalChecklistTask>? additionalTasks,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var preview = ChecklistTemplatePlanner.BuildFullTemplatePreview(weddingDate, now);
        var exclusions = excludeTitles ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var indexToTask = new Dictionary<int, EventTask>();
        var includedIndices = new List<int>();

        foreach (var plan in preview)
        {
            if (exclusions.Contains(plan.Title))
            {
                continue;
            }

            var task = new EventTask
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Title = plan.Title,
                Status = DomainTaskStatus.ToDo,
                StartDate = plan.StartDate,
                DueDate = plan.DueDate,
                CreatedAt = now,
                UpdatedAt = now
            };

            indexToTask[plan.TemplateIndex] = task;
            includedIndices.Add(plan.TemplateIndex);
            await taskRepository.AddAsync(task, cancellationToken);
        }

        var template = WeddingTaskTemplate.Entries;
        foreach (var index in includedIndices)
        {
            var dependsOnIndex = template[index].DependsOnTemplateIndex;
            if (dependsOnIndex is null)
            {
                continue;
            }

            var task = indexToTask[index];
            if (indexToTask.TryGetValue(dependsOnIndex.Value, out var dependency))
            {
                task.DependsOnTaskId = dependency.Id;
                continue;
            }

            var fallbackIndex = includedIndices.LastOrDefault(i => i <= dependsOnIndex.Value);
            if (indexToTask.TryGetValue(fallbackIndex, out var fallbackDep))
            {
                task.DependsOnTaskId = fallbackDep.Id;
            }
        }

        var orderedTemplateTasks = includedIndices.Select(i => indexToTask[i]).ToList();
        WeddingTaskScheduleCalculator.EnforceDependencyOrderByGraph(
            orderedTemplateTasks,
            weddingDate.Date,
            orderedTemplateTasks);

        foreach (var task in orderedTemplateTasks)
        {
            task.UpdatedAt = now;
        }

        if (additionalTasks is { Count: > 0 })
        {
            foreach (var extra in additionalTasks)
            {
                if (string.IsNullOrWhiteSpace(extra.Title))
                {
                    continue;
                }

                await taskRepository.AddAsync(
                    new EventTask
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Title = extra.Title.Trim(),
                        Description = string.IsNullOrWhiteSpace(extra.Description) ? null : extra.Description.Trim(),
                        Status = DomainTaskStatus.ToDo,
                        StartDate = extra.StartDate ?? now.Date,
                        DueDate = extra.DueDate ?? weddingDate.Date,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    cancellationToken);
            }
        }

        return includedIndices.Count + (additionalTasks?.Count ?? 0);
    }
}

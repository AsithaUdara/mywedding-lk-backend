using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Templates;

public static class PlannerCustomTemplateMaterializer
{
    public static PlannerTaskTemplate BuildTemplateFromEventTasks(
        string plannerId,
        string name,
        string? description,
        Guid sourceEventId,
        DateTime weddingDate,
        IReadOnlyList<EventTask> tasks)
    {
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException("Cannot save an empty checklist as a template.");
        }

        var wedding = weddingDate.Date;
        var ordered = tasks
            .OrderBy(t => t.StartDate ?? t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var taskIdToSortOrder = ordered
            .Select((task, index) => (task.Id, SortOrder: index))
            .ToDictionary(x => x.Id, x => x.SortOrder);

        var now = DateTime.UtcNow;
        var template = new PlannerTaskTemplate
        {
            Id = Guid.NewGuid(),
            PlannerId = plannerId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ScheduleMode = PlannerTaskTemplateScheduleMode.DaysBeforeWedding,
            SourceEventId = sourceEventId,
            CreatedAt = now,
            UpdatedAt = now
        };

        template.Items = ordered.Select((task, index) =>
        {
            var startOffset = task.StartDate.HasValue
                ? Math.Max(0, (int)(wedding - task.StartDate.Value.Date).TotalDays)
                : 0;
            var dueOffset = task.DueDate.HasValue
                ? Math.Max(0, (int)(wedding - task.DueDate.Value.Date).TotalDays)
                : startOffset;

            int? dependsOnSortOrder = null;
            if (task.DependsOnTaskId.HasValue
                && taskIdToSortOrder.TryGetValue(task.DependsOnTaskId.Value, out var depOrder))
            {
                dependsOnSortOrder = depOrder;
            }

            return new PlannerTaskTemplateItem
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                SortOrder = index,
                Title = task.Title,
                StartOffsetDays = startOffset,
                DueOffsetDays = Math.Max(dueOffset, startOffset),
                DependsOnSortOrder = dependsOnSortOrder
            };
        }).ToList();

        return template;
    }

    public static async Task<int> ApplyTemplateToEventAsync(
        PlannerTaskTemplate template,
        Guid eventId,
        DateTime weddingDate,
        IEventTaskRepository taskRepository,
        bool replaceExisting,
        CancellationToken cancellationToken)
    {
        if (template.Items.Count == 0)
        {
            return 0;
        }

        var existing = (await taskRepository.GetByEventIdAsync(eventId, cancellationToken)).ToList();
        if (existing.Count > 0)
        {
            if (!replaceExisting)
            {
                throw new InvalidOperationException(
                    "This event already has tasks. Apply with replaceExisting=true or use an empty timeline.");
            }

            foreach (var task in existing)
            {
                await taskRepository.DeleteAsync(task.Id, cancellationToken);
            }
        }

        var now = DateTime.UtcNow;
        var wedding = weddingDate.Date;
        var planStart = now.Date;
        var sortOrderToTask = new Dictionary<int, EventTask>();

        foreach (var item in template.Items.OrderBy(i => i.SortOrder))
        {
            DateTime startDate;
            DateTime dueDate;

            if (template.ScheduleMode == PlannerTaskTemplateScheduleMode.DaysFromPlanStart)
            {
                startDate = planStart.AddDays(item.StartOffsetDays);
                dueDate = planStart.AddDays(Math.Max(item.DueOffsetDays, item.StartOffsetDays + 1));
            }
            else
            {
                var window = WeddingTaskScheduleCalculator.ComputeScheduleDates(
                    wedding,
                    now,
                    item.StartOffsetDays,
                    item.DueOffsetDays);
                startDate = window.StartDate;
                dueDate = window.DueDate;
            }

            var task = new EventTask
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Title = item.Title,
                Status = DomainTaskStatus.ToDo,
                StartDate = startDate,
                DueDate = dueDate,
                CreatedAt = now,
                UpdatedAt = now
            };

            sortOrderToTask[item.SortOrder] = task;
            await taskRepository.AddAsync(task, cancellationToken);
        }

        foreach (var item in template.Items)
        {
            if (item.DependsOnSortOrder is null
                || !sortOrderToTask.TryGetValue(item.SortOrder, out var task)
                || !sortOrderToTask.TryGetValue(item.DependsOnSortOrder.Value, out var dependency))
            {
                continue;
            }

            task.DependsOnTaskId = dependency.Id;
            task.UpdatedAt = now;
            taskRepository.Update(task);
        }

        var orderedTasks = template.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => sortOrderToTask[i.SortOrder])
            .ToList();

        if (template.ScheduleMode == PlannerTaskTemplateScheduleMode.DaysBeforeWedding)
        {
            WeddingTaskScheduleCalculator.EnforceDependencyOrderByGraph(
                orderedTasks,
                wedding,
                orderedTasks);
        }

        foreach (var task in orderedTasks)
        {
            task.UpdatedAt = now;
            taskRepository.Update(task);
        }

        return template.Items.Count;
    }
}

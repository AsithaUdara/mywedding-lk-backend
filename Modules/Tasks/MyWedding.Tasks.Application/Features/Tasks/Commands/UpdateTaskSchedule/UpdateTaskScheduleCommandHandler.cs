using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTaskSchedule
{
    public class UpdateTaskScheduleCommandHandler : IRequestHandler<UpdateTaskScheduleCommand>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTaskScheduleCommandHandler(
            IEventTaskRepository taskRepository,
            IEventOrganizerRepository organizerRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _organizerRepository = organizerRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTaskScheduleCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("User must be authenticated to update tasks.");
            }

            var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
            if (task is null || task.EventId != request.EventId)
            {
                throw new NotFoundException($"Task with ID '{request.TaskId}' was not found for this event.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(
                request.EventId,
                request.UserId,
                cancellationToken);
            if (organizer is null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to update tasks for this event.");
            }

            if (request.UpdateDependency)
            {
                if (request.DependsOnTaskId == request.TaskId)
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        { "dependsOnTaskId", new[] { "A task cannot depend on itself." } }
                    });
                }

                if (request.DependsOnTaskId.HasValue)
                {
                    var dependency = await _taskRepository.GetByIdAsync(request.DependsOnTaskId.Value, cancellationToken);
                    if (dependency is null || dependency.EventId != request.EventId)
                    {
                        throw new ValidationException(new Dictionary<string, string[]>
                        {
                            { "dependsOnTaskId", new[] { "Dependency task must belong to the same event." } }
                        });
                    }

                    await EnsureNoCircularDependencyAsync(
                        request.EventId,
                        request.TaskId,
                        request.DependsOnTaskId.Value,
                        cancellationToken);
                }

                task.DependsOnTaskId = request.DependsOnTaskId;
            }

            var previousStart = task.StartDate;

            if (request.StartDate.HasValue)
            {
                task.StartDate = request.StartDate.Value;
            }

            if (request.DueDate.HasValue)
            {
                task.DueDate = request.DueDate.Value;
            }

            task.UpdatedAt = DateTime.UtcNow;
            _taskRepository.Update(task);

            if (request.StartDate.HasValue &&
                previousStart.HasValue &&
                task.StartDate.HasValue)
            {
                var shift = task.StartDate.Value - previousStart.Value;
                if (shift != TimeSpan.Zero)
                {
                    await CascadeDependentSchedulesAsync(
                        request.EventId,
                        request.TaskId,
                        shift,
                        new HashSet<Guid>(),
                        cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);
        }

        /// <summary>
        /// Walks the dependency chain upward from <paramref name="newDependencyId"/>; rejects if <paramref name="taskId"/> is reachable (would create a cycle).
        /// </summary>
        private async Task EnsureNoCircularDependencyAsync(
            Guid eventId,
            Guid taskId,
            Guid newDependencyId,
            CancellationToken cancellationToken)
        {
            var allTasks = (await _taskRepository.GetByEventIdAsync(eventId, cancellationToken)).ToList();
            var tasksById = allTasks.ToDictionary(t => t.Id);

            var current = newDependencyId;
            var visited = new HashSet<Guid>();

            while (true)
            {
                if (current == taskId)
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        {
                            "dependsOnTaskId",
                            new[] { "This dependency would create a circular task chain." }
                        }
                    });
                }

                if (!visited.Add(current))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        { "dependsOnTaskId", new[] { "Task dependencies already contain a cycle." } }
                    });
                }

                if (!tasksById.TryGetValue(current, out var currentTask) ||
                    !currentTask.DependsOnTaskId.HasValue)
                {
                    break;
                }

                current = currentTask.DependsOnTaskId.Value;
            }
        }

        /// <summary>
        /// Recursively shifts StartDate and DueDate for all tasks that depend on <paramref name="parentTaskId"/>.
        /// </summary>
        private async Task CascadeDependentSchedulesAsync(
            Guid eventId,
            Guid parentTaskId,
            TimeSpan shift,
            HashSet<Guid> visited,
            CancellationToken cancellationToken)
        {
            if (!visited.Add(parentTaskId))
            {
                return;
            }

            var allTasks = (await _taskRepository.GetByEventIdAsync(eventId, cancellationToken)).ToList();
            var dependents = allTasks.Where(t => t.DependsOnTaskId == parentTaskId).ToList();

            foreach (var dependent in dependents)
            {
                var tracked = await _taskRepository.GetByIdAsync(dependent.Id, cancellationToken);
                if (tracked is null)
                {
                    continue;
                }

                if (tracked.StartDate.HasValue)
                {
                    tracked.StartDate = tracked.StartDate.Value.Add(shift);
                }

                if (tracked.DueDate.HasValue)
                {
                    tracked.DueDate = tracked.DueDate.Value.Add(shift);
                }

                tracked.UpdatedAt = DateTime.UtcNow;
                _taskRepository.Update(tracked);

                await CascadeDependentSchedulesAsync(
                    eventId,
                    tracked.Id,
                    shift,
                    visited,
                    cancellationToken);
            }
        }
    }
}

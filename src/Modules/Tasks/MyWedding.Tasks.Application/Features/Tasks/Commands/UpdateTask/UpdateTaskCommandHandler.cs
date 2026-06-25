using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTaskCommandHandler(
            IEventTaskRepository taskRepository,
            IEventOrganizerRepository organizerRepository,
            IWeddingEventRepository eventRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _organizerRepository = organizerRepository;
            _eventRepository = eventRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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

            await EnsureCanManageTaskAsync(task.EventId, request.UserId, cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "title", new[] { "Task title is required." } }
                });
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

            task.Title = request.Title.Trim();
            task.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (request.Status.HasValue)
            {
                task.Status = request.Status.Value;
            }

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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);
        }

        private async Task EnsureCanManageTaskAsync(
            Guid eventId,
            string userId,
            CancellationToken cancellationToken)
        {
            var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId, cancellationToken);
            if (organizer is not null && organizer.PermissionLevel != MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                return;
            }

            var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(eventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Event '{eventId}' was not found.");
            }

            var canManageViaPlanner = weddingEvent.ManagingPlannerId == userId
                || weddingEvent.CreatedById == userId
                || await _eventRepository.IsManagedByPlannerAsync(eventId, userId, cancellationToken);

            if (!canManageViaPlanner)
            {
                throw new ForbiddenAccessException("You do not have permission to update tasks for this event.");
            }
        }

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
                        { "dependsOnTaskId", new[] { "This dependency would create a circular task chain." } }
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
    }
}

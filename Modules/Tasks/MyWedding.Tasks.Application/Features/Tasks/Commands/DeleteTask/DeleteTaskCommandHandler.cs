using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.DeleteTask
{
    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTaskCommandHandler(
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

        public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("User must be authenticated to delete tasks.");
            }

            var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
            if (task is null || task.EventId != request.EventId)
            {
                throw new NotFoundException($"Task with ID '{request.TaskId}' was not found for this event.");
            }

            await EnsureCanManageTaskAsync(task.EventId, request.UserId, cancellationToken);

            var allTasks = (await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken)).ToList();
            foreach (var dependent in allTasks.Where(t => t.DependsOnTaskId == request.TaskId))
            {
                var tracked = await _taskRepository.GetByIdAsync(dependent.Id, cancellationToken);
                if (tracked is null)
                {
                    continue;
                }

                tracked.DependsOnTaskId = null;
                tracked.UpdatedAt = DateTime.UtcNow;
                _taskRepository.Update(tracked);
            }

            await _taskRepository.DeleteAsync(request.TaskId, cancellationToken);
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
                throw new ForbiddenAccessException("You do not have permission to delete tasks for this event.");
            }
        }
    }
}

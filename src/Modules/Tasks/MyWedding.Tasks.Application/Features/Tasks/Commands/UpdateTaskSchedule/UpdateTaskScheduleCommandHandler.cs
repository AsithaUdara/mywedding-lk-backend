using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
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
                }

                task.DependsOnTaskId = request.DependsOnTaskId;
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
    }
}

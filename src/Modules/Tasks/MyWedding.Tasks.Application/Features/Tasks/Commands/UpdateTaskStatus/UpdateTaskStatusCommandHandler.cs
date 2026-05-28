using MediatR;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTaskStatusCommandHandler(
            IEventTaskRepository taskRepository,
            IEventOrganizerRepository organizerRepository,
            IAuditLogRepository auditLogRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _organizerRepository = organizerRepository;
            _auditLogRepository = auditLogRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
            {
                throw new NotFoundException($"Task with ID '{request.TaskId}' was not found.");
            }

            // --- SECURITY CHECK ---
            var organizer = await _organizerRepository.GetOrganizerAsync(task.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to update tasks for this event.");
            }

            task.Status = request.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;

            _taskRepository.Update(task);

            // --- CREATE ACTIVITY LOG ---
            if (request.NewStatus == DomainTaskStatus.Completed)
            {
                var auditItem = new AuditLogItem(
                    Guid.NewGuid(),
                    task.EventId,
                    request.UserId,
                    "TaskCompleted",
                    $"completed the task: \"{task.Title}\"",
                    null,
                    DateTime.UtcNow);
                await _auditLogRepository.AddAsync(auditItem, cancellationToken);
            }
            // --- END OF LOG ---

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyChecklistUpdatedAsync(task.EventId);
        }
    }
}

using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using MyWedding.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Guid>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTaskCommandHandler(
            IEventTaskRepository taskRepository, 
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new MyWedding.Application.Common.Exceptions.ForbiddenAccessException("User must be authenticated to create tasks.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new MyWedding.Application.Common.Exceptions.ForbiddenAccessException("You do not have permission to create tasks for this event.");
            }

            var newTask = new EventTask
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                Status = MyWedding.Domain.Enums.TaskStatus.ToDo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(newTask, cancellationToken);
            
            // Log to Activity Feed
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var activity = new ActivityFeedItem
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = request.UserId,
                    ItemType = MyWedding.Domain.Enums.ActivityType.SystemLog,
                    Content = $"New task created: \"{request.Title}\"",
                    CreatedAt = DateTime.UtcNow
                };
                await _activityFeedRepository.AddAsync(activity, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);
            
            return newTask.Id;
        }
    }
}
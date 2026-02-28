using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Guid>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTaskCommandHandler(
            IEventTaskRepository taskRepository, 
            IActivityFeedRepository activityFeedRepository,
            IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
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

            return newTask.Id;
        }
    }
}
using MediatR;




using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommandHandler : IRequestHandler<SetTotalBudgetCommand>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public SetTotalBudgetCommandHandler(
            IWeddingEventRepository weddingEventRepository, 
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SetTotalBudgetCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("User must be authenticated to modify budget.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to modify budget for this event.");
            }

            var weddingEvent = await _weddingEventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding event with ID '{request.EventId}' not found.");
            }

            weddingEvent.TotalBudget = request.TotalBudget;
            weddingEvent.UpdatedAt = DateTime.UtcNow;

            // Log to Activity Feed
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var activity = new ActivityFeedItem
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = request.UserId,
                    ItemType = MyWedding.Domain.Enums.ActivityType.SystemLog,
                    Content = $"Total budget updated to {request.TotalBudget:N0} LKR",
                    CreatedAt = DateTime.UtcNow
                };
                await _activityFeedRepository.AddAsync(activity, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyBudgetUpdatedAsync(request.EventId);
        }
    }
}

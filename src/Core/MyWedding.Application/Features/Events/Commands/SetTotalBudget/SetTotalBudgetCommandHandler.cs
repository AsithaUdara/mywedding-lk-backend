using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.Entities;
using MyWedding.Application.Common.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommandHandler : IRequestHandler<SetTotalBudgetCommand>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetTotalBudgetCommandHandler(
            IWeddingEventRepository weddingEventRepository, 
            IActivityFeedRepository activityFeedRepository,
            IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SetTotalBudgetCommand request, CancellationToken cancellationToken)
        {
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
        }
    }
}

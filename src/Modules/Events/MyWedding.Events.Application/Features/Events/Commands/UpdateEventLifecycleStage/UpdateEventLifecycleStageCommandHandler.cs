using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Events.Commands.UpdateEventLifecycleStage
{
    public class UpdateEventLifecycleStageCommandHandler : IRequestHandler<UpdateEventLifecycleStageCommand>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly ICurrentPlannerAccessor _currentPlannerAccessor;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateEventLifecycleStageCommandHandler(
            IWeddingEventRepository weddingEventRepository,
            ICurrentPlannerAccessor currentPlannerAccessor,
            IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _currentPlannerAccessor = currentPlannerAccessor;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateEventLifecycleStageCommand request, CancellationToken cancellationToken)
        {
            var plannerId = _currentPlannerAccessor.PlannerId;
            if (string.IsNullOrWhiteSpace(plannerId) || !_currentPlannerAccessor.IsPlanner)
            {
                throw new ForbiddenAccessException("Only planners can update event lifecycle stages.");
            }

            var weddingEvent = await _weddingEventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding event with ID '{request.EventId}' was not found.");
            }

            if (!string.Equals(weddingEvent.ManagingPlannerId, plannerId, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(weddingEvent.ManagingPlannerId))
                {
                    throw new ForbiddenAccessException("You do not manage this event.");
                }

                var canManage = await _weddingEventRepository.IsManagedByPlannerAsync(
                    request.EventId,
                    plannerId,
                    cancellationToken);
                if (!canManage)
                {
                    throw new ForbiddenAccessException("You do not manage this event.");
                }

                weddingEvent.ManagingPlannerId = plannerId;
            }

            weddingEvent.EventLifecycleStage = request.NewStage;
            weddingEvent.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

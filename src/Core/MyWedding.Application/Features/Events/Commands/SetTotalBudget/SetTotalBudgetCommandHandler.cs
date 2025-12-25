// File: src/Core/MyWedding.Application/Features/Events/Commands/SetTotalBudget/SetTotalBudgetCommandHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommandHandler : IRequestHandler<SetTotalBudgetCommand>
    {
        private readonly IWeddingEventRepository _weddingEventRepository; // We need this to find the event
        private readonly IUnitOfWork _unitOfWork;

        public SetTotalBudgetCommandHandler(IWeddingEventRepository weddingEventRepository, IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SetTotalBudgetCommand request, CancellationToken cancellationToken)
        {
            // Fetch the event
            var weddingEvent = await _weddingEventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding event with ID '{request.EventId}' not found.");
            }

            // Update the total budget
            weddingEvent.TotalBudget = request.TotalBudget;
            weddingEvent.UpdatedAt = DateTime.UtcNow; // Update timestamp

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

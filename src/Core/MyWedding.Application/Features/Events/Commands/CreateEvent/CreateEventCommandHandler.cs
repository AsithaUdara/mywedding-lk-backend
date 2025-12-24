// File: src/Core/MyWedding.Application/Features/Events/Commands/CreateEvent/CreateEventCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEventCommandHandler(IWeddingEventRepository weddingEventRepository, IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var newEvent = new WeddingEvent
            {
                Id = Guid.NewGuid(),
                EventName = request.EventName,
                EventDate = request.EventDate,
                CreatedById = request.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _weddingEventRepository.AddAsync(newEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newEvent.Id;
        }
    }
}

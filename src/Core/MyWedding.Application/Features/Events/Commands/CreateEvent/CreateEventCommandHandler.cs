// File: src/Core/MyWedding.Application/Features/Events/Commands/CreateEvent/CreateEventCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEventCommandHandler(
            IWeddingEventRepository weddingEventRepository,
            IEventOrganizerRepository organizerRepository,
            IConversationRepository conversationRepository,
            IUnitOfWork unitOfWork)
        {
            _weddingEventRepository = weddingEventRepository;
            _organizerRepository = organizerRepository;
            _conversationRepository = conversationRepository;
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

            // Automatically add creator as owner-level organizer
            var ownerAsOrganizer = new EventOrganizer
            {
                EventId = newEvent.Id,
                UserId = request.UserId,
                Role = OrganizerRole.Bride,
                PermissionLevel = PermissionLevel.Owner,
                JoinedAt = DateTime.UtcNow
            };

            await _organizerRepository.AddAsync(ownerAsOrganizer, cancellationToken);

            // Auto-create default conversation channels for collaboration
            var generalChannel = new Conversation
            {
                Id = Guid.NewGuid(),
                Name = "general",
                EventId = newEvent.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _conversationRepository.AddAsync(generalChannel, cancellationToken);

            var planningChannel = new Conversation
            {
                Id = Guid.NewGuid(),
                Name = "planning",
                EventId = newEvent.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _conversationRepository.AddAsync(planningChannel, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newEvent.Id;
        }
    }
}

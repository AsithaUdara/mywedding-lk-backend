// File: src/Core/MyWedding.Application/Features/Events/Commands/SetEventPreferences/SetEventPreferencesCommandHandler.cs
using MediatR;
using MyWedding.Application.Common.Exceptions;
using MyWedding.Domain.Interfaces;
using System;
using System.Text.Json; // The modern .NET JSON library
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Commands.SetEventPreferences
{
    public class SetEventPreferencesCommandHandler : IRequestHandler<SetEventPreferencesCommand>
    {
        private readonly IWeddingEventRepository _eventRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetEventPreferencesCommandHandler(
            IWeddingEventRepository eventRepository,
            IEventOrganizerRepository organizerRepository,
            IUnitOfWork unitOfWork)
        {
            _eventRepository = eventRepository;
            _organizerRepository = organizerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SetEventPreferencesCommand request, CancellationToken cancellationToken)
        {
            // 1. Security Check: Does the user have permission to edit this event?
            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (organizer is null || organizer.PermissionLevel < Domain.Enums.PermissionLevel.Editor)
            {
                throw new ForbiddenAccessException("You do not have permission to edit this event's preferences.");
            }

            // 2. Find the event to update
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding Event with ID '{request.EventId}' not found.");
            }

            // 3. Convert the preferences dictionary to a JSON string
            var preferencesJson = JsonSerializer.Serialize(request.Preferences);

            // 4. Update the entity
            weddingEvent.StylePreferences = preferencesJson;
            weddingEvent.UpdatedAt = DateTime.UtcNow;

            // 5. Save the changes to the database
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

// File: src/Core/MyWedding.Application/Features/Events/Commands/CreateEvent/CreateEventCommand.cs
using MediatR;
using System;

namespace MyWedding.Events.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommand : IRequest<Guid>
    {
        public required string EventName { get; init; }
        public DateTime EventDate { get; init; }
        public required string UserId { get; init; } // The ID of the user creating the event
    }
}

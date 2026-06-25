// File: src/Core/MyWedding.Application/Features/EventOrganizers/Commands/InviteUserToEvent/InviteUserToEventCommand.cs
using MediatR;

using System;

namespace MyWedding.Events.Application.Features.EventOrganizers.Commands.InviteUserToEvent
{
    public class InviteUserToEventCommand : IRequest
    {
        public Guid EventId { get; init; }
        public required string InviteeEmail { get; init; }
        public OrganizerRole Role { get; init; }
        public PermissionLevel PermissionLevel { get; init; }
        public required string InviterUserId { get; init; } // The person SENDING the invite
    }
}

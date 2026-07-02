using MediatR;

using System;

namespace MyWedding.Events.Application.Features.EventOrganizers.Commands.UpdateOrganizer
{
    public class UpdateOrganizerCommand : IRequest
    {
        public Guid EventId { get; set; }
        public required string TargetUserId { get; set; }
        public required string RequestingUserId { get; set; }
        public OrganizerRole Role { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
    }
}

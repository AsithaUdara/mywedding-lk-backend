using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Application.Features.Invitations.Queries.GetEventInvitations
{
    public class GetEventInvitationsQuery : IRequest<IEnumerable<InvitationDto>>
    {
        public Guid EventId { get; set; }
    }

    public class InvitationDto
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public DateTime InvitedAt { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsExpired { get; set; }
    }
}

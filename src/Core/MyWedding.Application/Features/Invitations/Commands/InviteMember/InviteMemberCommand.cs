using MediatR;
using System;

namespace MyWedding.Application.Features.Invitations.Commands.InviteMember
{
    public class InviteMemberCommand : IRequest<Guid>
    {
        public Guid EventId { get; set; }
        public required string Email { get; set; }
        public required string InvitedById { get; set; }
    }
}

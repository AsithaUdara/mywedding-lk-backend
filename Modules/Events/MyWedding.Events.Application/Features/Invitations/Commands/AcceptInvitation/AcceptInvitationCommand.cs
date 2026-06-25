using MediatR;
using System;

namespace MyWedding.Events.Application.Features.Invitations.Commands.AcceptInvitation
{
    public class AcceptInvitationCommand : IRequest<bool>
    {
        public required string Token { get; set; }
        public required string UserId { get; set; }
    }
}

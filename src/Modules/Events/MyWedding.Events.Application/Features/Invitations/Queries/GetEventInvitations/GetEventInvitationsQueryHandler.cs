using MediatR;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Invitations.Queries.GetEventInvitations
{
    public class GetEventInvitationsQueryHandler : IRequestHandler<GetEventInvitationsQuery, IEnumerable<InvitationDto>>
    {
        private readonly IEventInvitationRepository _invitationRepository;

        public GetEventInvitationsQueryHandler(IEventInvitationRepository invitationRepository)
        {
            _invitationRepository = invitationRepository;
        }

        public async Task<IEnumerable<InvitationDto>> Handle(GetEventInvitationsQuery request, CancellationToken cancellationToken)
        {
            var invitations = await _invitationRepository.GetByEventIdAsync(request.EventId, cancellationToken);
            
            return invitations.Select(i => new InvitationDto
            {
                Id = i.Id,
                Email = i.Email,
                InvitedAt = i.InvitedAt,
                IsAccepted = i.IsAccepted,
                AcceptedAt = i.AcceptedAt,
                IsExpired = i.IsExpired
            });
        }
    }
}

// File: .../GetConversationsByEventId/GetConversationsByEventIdQueryHandler.cs
using MediatR;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.Conversations.Queries.GetConversationsByEventId
{
    public class GetConversationsByEventIdQueryHandler : IRequestHandler<GetConversationsByEventIdQuery, IEnumerable<ConversationDto>>
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;

        public GetConversationsByEventIdQueryHandler(IConversationRepository conversationRepository, IEventOrganizerRepository organizerRepository)
        {
            _conversationRepository = conversationRepository;
            _organizerRepository = organizerRepository;
        }

        public async Task<IEnumerable<ConversationDto>> Handle(GetConversationsByEventIdQuery request, CancellationToken cancellationToken)
        {
            // Security Check: Is the user a member of this event?
            var isMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (!isMember)
            {
                throw new ForbiddenAccessException("You do not have permission to view conversations for this event.");
            }

            var conversations = await _conversationRepository.GetByEventIdAsync(request.EventId, cancellationToken);

            return conversations.Select(c => new ConversationDto(c.Id, c.Name));
        }
    }
}

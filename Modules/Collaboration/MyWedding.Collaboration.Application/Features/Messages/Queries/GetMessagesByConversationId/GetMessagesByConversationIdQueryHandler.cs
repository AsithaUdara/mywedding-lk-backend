using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.Messages.Queries.GetMessagesByConversationId
{
    public class GetMessagesByConversationIdQueryHandler : IRequestHandler<GetMessagesByConversationIdQuery, IEnumerable<MessageDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;

        public GetMessagesByConversationIdQueryHandler(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IEventOrganizerRepository organizerRepository)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _organizerRepository = organizerRepository;
        }

        public async Task<IEnumerable<MessageDto>> Handle(GetMessagesByConversationIdQuery request, CancellationToken cancellationToken)
        {
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                throw new NotFoundException($"Conversation with ID '{request.ConversationId}' not found.");
            }

            var isParticipant = await _organizerRepository.IsUserAlreadyOrganizerAsync(
                conversation.EventId,
                request.UserId,
                cancellationToken);

            if (!isParticipant)
            {
                throw new ForbiddenAccessException("You do not have permission to view messages in this conversation.");
            }

            var messages = await _messageRepository.GetByConversationIdAsync(request.ConversationId, cancellationToken);
            return messages.Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.CreatedAt,
                m.Sender!.Id,
                m.Sender.FirstName,
                m.Sender.LastName,
                m.Sender.Email,
                null
            ));
        }
    }
}

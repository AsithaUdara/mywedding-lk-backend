// File: .../GetMessagesByConversationId/GetMessagesByConversationIdQueryHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.Messages.Queries.GetMessagesByConversationId
{
    public class GetMessagesByConversationIdQueryHandler : IRequestHandler<GetMessagesByConversationIdQuery, IEnumerable<MessageDto>>
    {
        private readonly IMessageRepository _messageRepository;
        // TODO: Add security check to ensure user belongs to this conversation's event

        public GetMessagesByConversationIdQueryHandler(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<IEnumerable<MessageDto>> Handle(GetMessagesByConversationIdQuery request, CancellationToken cancellationToken)
        {
            var messages = await _messageRepository.GetByConversationIdAsync(request.ConversationId, cancellationToken);
            return messages.Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.CreatedAt,
                m.Sender!.Id,
                m.Sender.FirstName,
                m.Sender.LastName,
                null // Placeholder for attachments
            ));
        }
    }
}

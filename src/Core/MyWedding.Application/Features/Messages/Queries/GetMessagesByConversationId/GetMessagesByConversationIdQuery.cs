// File: .../GetMessagesByConversationId/GetMessagesByConversationIdQuery.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Application.Features.Messages.Queries.GetMessagesByConversationId
{
    public class GetMessagesByConversationIdQuery : IRequest<IEnumerable<MessageDto>>
    {
        public Guid ConversationId { get; init; }
        public required string UserId { get; init; } // For security check
    }
}

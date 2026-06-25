// File: .../GetConversationsByEventId/GetConversationsByEventIdQuery.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Collaboration.Application.Features.Conversations.Queries.GetConversationsByEventId
{
    public class GetConversationsByEventIdQuery : IRequest<IEnumerable<ConversationDto>>
    {
        public Guid EventId { get; init; }
        public required string UserId { get; init; } // For security check
    }
}

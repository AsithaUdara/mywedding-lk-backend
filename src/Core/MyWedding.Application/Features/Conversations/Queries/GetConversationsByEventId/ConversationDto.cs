// File: src/Core/MyWedding.Application/Features/Conversations/Queries/GetConversationsByEventId/ConversationDto.cs
using System;

namespace MyWedding.Application.Features.Conversations.Queries.GetConversationsByEventId
{
    public record ConversationDto(Guid Id, string Name);
}

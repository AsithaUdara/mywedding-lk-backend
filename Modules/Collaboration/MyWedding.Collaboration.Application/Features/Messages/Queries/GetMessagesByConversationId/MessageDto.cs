// File: src/Core/MyWedding.Application/Features/Messages/Queries/GetMessagesByConversationId/MessageDto.cs
using System;

namespace MyWedding.Collaboration.Application.Features.Messages.Queries.GetMessagesByConversationId
{
    public record MessageDto(
        Guid Id,
        string Content,
        DateTime CreatedAt,
        string SenderId,
        string SenderFirstName,
        string SenderLastName,
        string SenderEmail,
        // We can add attachment details here later
        object? Attachment 
    );
}

// File: .../PostMessage/PostMessageCommand.cs
using MediatR;
using System;

namespace MyWedding.Collaboration.Application.Features.Messages.Commands.PostMessage
{
    public class PostMessageCommand : IRequest<Guid>
    {
        public Guid ConversationId { get; init; }
        public required string SenderId { get; init; }
        public required string Content { get; init; }

        // Optional "Smart Attachment" IDs
        public Guid? AttachedVendorServiceId { get; init; }
        public Guid? AttachedEventTaskId { get; init; }
        public Guid? AttachedExpenseId { get; init; }
    }
}

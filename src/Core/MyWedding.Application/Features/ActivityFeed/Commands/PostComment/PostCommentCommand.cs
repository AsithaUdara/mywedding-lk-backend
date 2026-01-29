// File: src/Core/MyWedding.Application/Features/ActivityFeed/Commands/PostComment/PostCommentCommand.cs
using MediatR;
using System;

namespace MyWedding.Application.Features.ActivityFeed.Commands.PostComment
{
    public class PostCommentCommand : IRequest<Guid>
    {
        public Guid EventId { get; init; }
        public required string UserId { get; init; }
        public required string Content { get; init; }
    }
}

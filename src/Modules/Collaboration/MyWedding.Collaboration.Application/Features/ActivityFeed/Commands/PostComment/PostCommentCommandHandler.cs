// File: src/Core/MyWedding.Application/Features/ActivityFeed/Commands/PostComment/PostCommentCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.ActivityFeed.Commands.PostComment
{
    public class PostCommentCommandHandler : IRequestHandler<PostCommentCommand, Guid>
    {
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommentCommandHandler(IActivityFeedRepository activityFeedRepository, IUnitOfWork unitOfWork)
        {
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(PostCommentCommand request, CancellationToken cancellationToken)
        {
            // TODO: Add security check to ensure user is a member of the event
            var newComment = new ActivityFeedItem
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                UserId = request.UserId,
                ItemType = MyWedding.Domain.Enums.ActivityType.UserComment,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _activityFeedRepository.AddAsync(newComment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newComment.Id;
        }
    }
}

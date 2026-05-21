// File: src/Core/MyWedding.Application/Features/ActivityFeed/Commands/PostComment/PostCommentCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.ActivityFeed.Commands.PostComment
{
    public class PostCommentCommandHandler : IRequestHandler<PostCommentCommand, Guid>
    {
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommentCommandHandler(
            IActivityFeedRepository activityFeedRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _activityFeedRepository = activityFeedRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(PostCommentCommand request, CancellationToken cancellationToken)
        {
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

            // Broadcast real-time activity update so all event members see the new comment instantly
            await _collaborationService.NotifyActivityAsync(request.EventId, new
            {
                id = newComment.Id,
                itemType = newComment.ItemType.ToString(),
                content = newComment.Content,
                createdAt = newComment.CreatedAt,
                userId = newComment.UserId
            });

            return newComment.Id;
        }
    }
}

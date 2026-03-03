using MediatR;
using MyWedding.Application.Common.Exceptions;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Polls.Commands.CreatePoll
{
    public class CreatePollCommandHandler : IRequestHandler<CreatePollCommand, Guid>
    {
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MyWedding.Infrastructure.Persistence.ApplicationDbContext _context; // Temporary context access until PollRepository is created

        public CreatePollCommandHandler(
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            IUserRepository userRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork,
            MyWedding.Infrastructure.Persistence.ApplicationDbContext context)
        {
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _userRepository = userRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<Guid> Handle(CreatePollCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("User must be authenticated to create polls.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to create polls for this event.");
            }

            // --- CREATE POLL ---
            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                EventId = request.EventId,
                CreatedById = request.UserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Options = request.Options.Select(o => new PollOption 
                { 
                    Id = Guid.NewGuid(), 
                    OptionText = o 
                }).ToList()
            };

            await _context.Polls.AddAsync(poll, cancellationToken);

            // --- CREATE ACTIVITY LOG ---
            var activityItem = new ActivityFeedItem
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                UserId = request.UserId,
                ItemType = MyWedding.Domain.Enums.ActivityType.SystemLog,
                Content = $"created a new poll: \"{request.Title}\"",
                CreatedAt = DateTime.UtcNow
            };
            await _activityFeedRepository.AddAsync(activityItem, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fetch user for real-time activity update
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyPollsUpdatedAsync(request.EventId);
            await _collaborationService.NotifyActivityAsync(request.EventId, new
            {
                id = activityItem.Id,
                userId = activityItem.UserId,
                userFirstName = user?.FirstName ?? "Team",
                userLastName = user?.LastName ?? "Member",
                itemType = activityItem.ItemType.ToString(),
                content = activityItem.Content,
                createdAt = activityItem.CreatedAt
            });

            return poll.Id;
        }
    }
}

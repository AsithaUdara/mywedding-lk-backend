// File: src/Core/MyWedding.Application/Features/EventOrganizers/Commands/InviteUserToEvent/InviteUserToEventCommandHandler.cs
using MediatR;
using MyWedding.Application.Common.Exceptions;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.EventOrganizers.Commands.InviteUserToEvent
{
    public class InviteUserToEventCommandHandler : IRequestHandler<InviteUserToEventCommand>
    {
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InviteUserToEventCommandHandler(
            IEventOrganizerRepository organizerRepository,
            IUserRepository userRepository,
            IActivityFeedRepository activityFeedRepository,
            IUnitOfWork unitOfWork)
        {
            _organizerRepository = organizerRepository;
            _userRepository = userRepository;
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(InviteUserToEventCommand request, CancellationToken cancellationToken)
        {
            // 1. Security Check: Does the person sending the invite have permission?
            var inviter = await _organizerRepository.GetOrganizerAsync(request.EventId, request.InviterUserId, cancellationToken);
            if (inviter is null || (inviter.PermissionLevel != Domain.Enums.PermissionLevel.Editor && inviter.PermissionLevel != Domain.Enums.PermissionLevel.Owner))
            {
                throw new ForbiddenAccessException("You do not have permission to invite members to this event.");
            }

            // 2. Find the user who is being invited
            var invitee = await _userRepository.GetByEmailAsync(request.InviteeEmail, cancellationToken);
            if (invitee is null)
            {
                throw new NotFoundException($"User with email '{request.InviteeEmail}' was not found.");
            }

            if (inviter.UserId == invitee.Id)
            {
                throw new InvalidOperationException("You cannot invite yourself to the event.");
            }

            // 3. Check if the user is already a member of this event
            var isAlreadyMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(request.EventId, invitee.Id, cancellationToken);
            if (isAlreadyMember)
            {
                throw new InvalidOperationException("This user is already a member of the event.");
            }

            // 4. All checks passed. Create the new EventOrganizer entity.
            var newOrganizer = new EventOrganizer
            {
                EventId = request.EventId,
                UserId = invitee.Id,
                Role = request.Role,
                PermissionLevel = request.PermissionLevel,
                JoinedAt = DateTime.UtcNow
            };

            // 5. Add to the database
            await _organizerRepository.AddAsync(newOrganizer, cancellationToken);

            // 6. Log activity: User was invited to the team
            var activityItem = new ActivityFeedItem
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                UserId = request.InviterUserId,
                ItemType = Domain.Enums.ActivityType.SystemLog,
                Content = $"invited {invitee.FirstName} to the planning team",
                CreatedAt = DateTime.UtcNow
            };
            await _activityFeedRepository.AddAsync(activityItem, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Invitations.Commands.AcceptInvitation
{
    public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, bool>
    {
        private readonly IEventInvitationRepository _invitationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AcceptInvitationCommandHandler(
            IEventInvitationRepository invitationRepository,
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            IUnitOfWork unitOfWork)
        {
            _invitationRepository = invitationRepository;
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);

            if (invitation == null)
            {
                return false;
            }

            // 1. Check if user is ALREADY an organizer (Idempotency)
            // If the user effectively "owns" this spot already, treat it as a success.
            // This handles cases where they click the link again or the UI didn't update.
            var alreadyOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(invitation.EventId, request.UserId, cancellationToken);
            if (alreadyOrganizer)
            {
                return true;
            }

            // 2. NOW check if validation fails
            if (invitation.IsAccepted || invitation.IsExpired)
            {
                return false;
            }

            // 3. Add user as an organizer (if not already)
            // (alreadyOrganizer check removed from here since we did it above)
            {
                var organizer = new EventOrganizer
                {
                    EventId = invitation.EventId,
                    UserId = request.UserId,
                    Role = OrganizerRole.Friend, // Default role
                    PermissionLevel = PermissionLevel.Editor, // Default permission
                    JoinedAt = DateTime.UtcNow
                };
                await _organizerRepository.AddAsync(organizer, cancellationToken);
            }

            // 2. Update invitation status
            invitation.IsAccepted = true;
            invitation.AcceptedAt = DateTime.UtcNow;
            _invitationRepository.Update(invitation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Post to Activity Feed
            var activity = new ActivityFeedItem
            {
                Id = Guid.NewGuid(),
                EventId = invitation.EventId,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                ItemType = MyWedding.Domain.Enums.ActivityType.SystemLog,
                Content = "joined the wedding planning team."
            };
            await _activityFeedRepository.AddAsync(activity, cancellationToken);
            
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Verify if the user is now an organizer (handle race condition/idempotency)
                // We catch generic Exception because we cannot easily reference EF Core exceptions here
                // If the insert failed for ANY reason but the user is NOW an organizer, we consider it a success.
                var isNowOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(invitation.EventId, request.UserId, cancellationToken);
                if (isNowOrganizer)
                {
                    return true;
                }
                throw; // Rethrow if it wasn't a duplicate key or user is still not an organizer
            }

            return true;
        }
    }
}

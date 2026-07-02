using MediatR;



using System;
using System.Threading;
using System.Threading.Tasks;


namespace MyWedding.Events.Application.Features.Invitations.Commands.AcceptInvitation
{
    public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, bool>
    {
        private readonly IEventInvitationRepository _invitationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public AcceptInvitationCommandHandler(
            IEventInvitationRepository invitationRepository,
            IEventOrganizerRepository organizerRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _invitationRepository = invitationRepository;
            _organizerRepository = organizerRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            var userId = request.UserId?.Trim();
            if (string.IsNullOrEmpty(userId))
            {
                throw new MyWedding.SharedKernel.Exceptions.ForbiddenAccessException("User ID is required.");
            }

            var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);

            if (invitation == null)
            {
                return false;
            }

            // 1. Check if user is ALREADY an organizer (Idempotency)
            var alreadyOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(invitation.EventId, userId, cancellationToken);
            if (alreadyOrganizer)
            {
                // If they are already a member, we just mark the invitation as accepted if it isn't already
                if (!invitation.IsAccepted)
                {
                    invitation.IsAccepted = true;
                    invitation.AcceptedAt = DateTime.UtcNow;
                    _invitationRepository.Update(invitation);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return true;
            }

            // 2. Validate invitation status
            if (invitation.IsAccepted || invitation.IsExpired)
            {
                return false;
            }

            // 3. Ensure User exists in local DB (Foreign Key requirement)
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                // If user is logged in but not in our DB, they might need to complete profile/registration
                throw new MyWedding.SharedKernel.Exceptions.NotFoundException("User not found in local database. Please ensure your profile is created before joining a team.");
            }

            // 4. Update invitation status
            invitation.IsAccepted = true;
            invitation.AcceptedAt = DateTime.UtcNow;
            _invitationRepository.Update(invitation);

            // 5. Add user as an organizer
            var organizer = new EventOrganizer
            {
                EventId = invitation.EventId,
                UserId = userId,
                Role = invitation.Role,
                PermissionLevel = invitation.PermissionLevel,
                JoinedAt = DateTime.UtcNow
            };
            await _organizerRepository.AddAsync(organizer, cancellationToken);

            // 6. Post to Activity Feed
            var auditItem = new AuditLogItem(
                Guid.NewGuid(),
                invitation.EventId,
                userId,
                "TeamMemberJoined",
                "joined the wedding planning team.",
                null,
                DateTime.UtcNow);
            await _auditLogRepository.AddAsync(auditItem, cancellationToken);
            
            try 
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle potential race condition where they joined via another window at the same time
                var isNowOrganizer = await _organizerRepository.IsUserAlreadyOrganizerAsync(invitation.EventId, userId, cancellationToken);
                if (isNowOrganizer)
                {
                    return true;
                }
                
                // If it's not a duplicate key error, wrap it with more context
                throw new Exception($"Failed to join the team: {ex.Message}. This usually happens if there is a data constraint violation.", ex);
            }

            // 7. Real-time Notifications
            try
            {
                // Notify team that someone joined (triggers refresh for others)
                await _collaborationService.NotifyActivityAsync(invitation.EventId, new
                {
                    id = auditItem.Id,
                    userId = auditItem.ActorId,
                    userFirstName = user.FirstName,
                    userLastName = user.LastName,
                    itemType = auditItem.ActionType,
                    content = auditItem.Content,
                    createdAt = auditItem.TimestampUtc
                });

                // Specific signal for invitations status refresh
                await _collaborationService.NotifyInvitationAcceptedAsync(invitation.EventId, invitation.Email);
            }
            catch (Exception)
            {
                // Don't fail the whole operation if notification fails
            }

            return true;
        }
    }
}

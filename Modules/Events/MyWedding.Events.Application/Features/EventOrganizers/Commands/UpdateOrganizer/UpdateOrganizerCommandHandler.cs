using MediatR;



using System.Threading;
using System.Threading.Tasks;


namespace MyWedding.Events.Application.Features.EventOrganizers.Commands.UpdateOrganizer
{
    public class UpdateOrganizerCommandHandler : IRequestHandler<UpdateOrganizerCommand>
    {
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICollaborationService _collaborationService;

        public UpdateOrganizerCommandHandler(
            IEventOrganizerRepository organizerRepository, 
            IUnitOfWork unitOfWork,
            ICollaborationService collaborationService)
        {
            _organizerRepository = organizerRepository;
            _unitOfWork = unitOfWork;
            _collaborationService = collaborationService;
        }

        public async Task Handle(UpdateOrganizerCommand request, CancellationToken cancellationToken)
        {
            // 1. Check if requester has permission (must be Owner or Editor to change roles? Let's say Owner only to be safe, or Editor)
            var requester = await _organizerRepository.GetOrganizerAsync(request.EventId, request.RequestingUserId, cancellationToken);
            if (requester == null || requester.Role != OrganizerRole.Owner)
            {
                // Only Owner can modify roles
                throw new ForbiddenAccessException("Only the Event Owner can change member roles and permissions.");
            }

            // 2. Prevent Owner from demoting themselves
            if (request.RequestingUserId == request.TargetUserId && request.Role != OrganizerRole.Owner)
            {
                throw new ForbiddenAccessException("You cannot demote yourself from the Owner role.");
            }

            // 3. Find the target member
            var targetOrganizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.TargetUserId, cancellationToken);
            if (targetOrganizer == null)
            {
                throw new NotFoundException("Team member not found in this event.");
            }

            // 4. Update role and permission
            targetOrganizer.Role = request.Role;
            targetOrganizer.PermissionLevel = request.PermissionLevel;

            _organizerRepository.Update(targetOrganizer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify real-time clients that team was updated
            await _collaborationService.NotifyInvitationAcceptedAsync(request.EventId, "Member role updated");
        }
    }
}

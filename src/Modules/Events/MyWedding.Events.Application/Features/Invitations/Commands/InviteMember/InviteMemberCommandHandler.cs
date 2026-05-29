using MediatR;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;


namespace MyWedding.Events.Application.Features.Invitations.Commands.InviteMember
{
    public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Guid>
    {
        private readonly IEventInvitationRepository _invitationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public InviteMemberCommandHandler(
            IEventInvitationRepository invitationRepository, 
            IEventOrganizerRepository organizerRepository,
            IWeddingEventRepository eventRepository,
            ICollaborationService collaborationService,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _invitationRepository = invitationRepository;
            _organizerRepository = organizerRepository;
            _eventRepository = eventRepository;
            _collaborationService = collaborationService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.InvitedById, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new MyWedding.SharedKernel.Exceptions.ForbiddenAccessException("You do not have permission to invite members to this event.");
            }

            // 1. Generate a secure random token
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            // 2. Create the invitation record
            var invitation = new EventInvitation
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Email = request.Email,
                Token = token,
                InvitedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Expire in 7 days
                IsAccepted = false,
                InvitedById = request.InvitedById,
                Role = request.Role,
                PermissionLevel = request.PermissionLevel
            };

            await _invitationRepository.AddAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            await _emailService.SendEventInvitationAsync(
                new EventInvitationEmailMessage(
                    request.Email,
                    weddingEvent?.EventName ?? "Your wedding",
                    request.EventId,
                    token,
                    request.Role.ToString(),
                    request.PermissionLevel.ToString()),
                cancellationToken);

            await _collaborationService.NotifyInvitationAcceptedAsync(request.EventId, request.Email);

            return invitation.Id;
        }
    }
}

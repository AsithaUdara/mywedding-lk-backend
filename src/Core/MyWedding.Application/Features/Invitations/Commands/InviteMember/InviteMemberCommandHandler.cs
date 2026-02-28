using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Invitations.Commands.InviteMember
{
    public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Guid>
    {
        private readonly IEventInvitationRepository _invitationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InviteMemberCommandHandler(IEventInvitationRepository invitationRepository, IUnitOfWork unitOfWork)
        {
            _invitationRepository = invitationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
        {
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
                InvitedById = request.InvitedById
            };

            await _invitationRepository.AddAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Mock Email Sending
            // In a real app, you would inject an IEmailService and call it here:
            // await _emailService.SendInvitationEmail(request.Email, token, request.EventId);
            Console.WriteLine($"[MOCK EMAIL] Invitation sent to {request.Email} with token {token}");

            return invitation.Id;
        }
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Application.Common.Exceptions;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Polls.Commands.Vote
{
    public class VoteCommandHandler : IRequestHandler<VoteCommand, Unit>
    {
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MyWedding.Infrastructure.Persistence.ApplicationDbContext _context;

        public VoteCommandHandler(
            IEventOrganizerRepository organizerRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork,
            MyWedding.Infrastructure.Persistence.ApplicationDbContext context)
        {
            _organizerRepository = organizerRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<Unit> Handle(VoteCommand request, CancellationToken cancellationToken)
        {
            // --- FIND THE POLL ---
            var poll = await _context.Polls
                .FirstOrDefaultAsync(p => p.Id == request.PollId, cancellationToken);

            if (poll == null)
            {
                throw new NotFoundException($"Poll with ID '{request.PollId}' not found.");
            }

            // --- SECURITY CHECK ---
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("User must be authenticated to vote.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(poll.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to vote in this event.");
            }

            // --- VOTE LOGIC ---
            // Check if user already voted in this poll
            var existingVote = await _context.PollVotes
                .FirstOrDefaultAsync(v => v.PollOption!.PollId == request.PollId && v.UserId == request.UserId, cancellationToken);

            if (existingVote != null)
            {
                _context.PollVotes.Remove(existingVote); // Toggle logic: remove existing vote
                
                // If the user is voting for the SAME option, we just removed it (toggle off).
                // If they are voting for a DIFFERENT option, we continue to add the new vote.
                if (existingVote.PollOptionId == request.OptionId)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    // Notify real-time clients
                    await _collaborationService.NotifyPollsUpdatedAsync(poll.EventId);
                    return Unit.Value;
                }
            }

            var vote = new PollVote
            {
                Id = Guid.NewGuid(),
                PollOptionId = request.OptionId,
                UserId = request.UserId,
                VotedAt = DateTime.UtcNow
            };

            await _context.PollVotes.AddAsync(vote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyPollsUpdatedAsync(poll.EventId);

            return Unit.Value;
        }
    }
}

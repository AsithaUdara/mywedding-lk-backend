using Microsoft.EntityFrameworkCore;


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Infrastructure.Persistence.Repositories
{
    public class PollRepository : IPollRepository
    {
        private readonly ApplicationDbContext _context;

        public PollRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Poll?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Polls
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<PollVote?> GetVoteByUserInPollAsync(Guid pollId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.PollVotes
                .FirstOrDefaultAsync(v => v.PollOption!.PollId == pollId && v.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(Poll poll, CancellationToken cancellationToken = default)
        {
            await _context.Polls.AddAsync(poll, cancellationToken);
        }

        public async Task AddVoteAsync(PollVote vote, CancellationToken cancellationToken = default)
        {
            await _context.PollVotes.AddAsync(vote, cancellationToken);
        }

        public void RemoveVote(PollVote vote)
        {
            _context.PollVotes.Remove(vote);
        }
    }
}

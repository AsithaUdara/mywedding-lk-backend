using MyWedding.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IPollRepository
    {
        Task<Poll?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PollVote?> GetVoteByUserInPollAsync(Guid pollId, string userId, CancellationToken cancellationToken = default);
        Task AddAsync(Poll poll, CancellationToken cancellationToken = default);
        Task AddVoteAsync(PollVote vote, CancellationToken cancellationToken = default);
        void RemoveVote(PollVote vote);
    }
}

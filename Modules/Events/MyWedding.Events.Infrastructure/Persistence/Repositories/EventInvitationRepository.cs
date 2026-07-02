using Microsoft.EntityFrameworkCore;


using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Infrastructure.Persistence.Repositories
{
    public class EventInvitationRepository : IEventInvitationRepository
    {
        private readonly ApplicationDbContext _context;

        public EventInvitationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EventInvitation invitation, CancellationToken cancellationToken = default)
        {
            await _context.EventInvitations.AddAsync(invitation, cancellationToken);
        }

        public async Task<EventInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.EventInvitations
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
        }

        public async Task<IEnumerable<EventInvitation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.EventInvitations
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.InvitedAt)
                .ToListAsync(cancellationToken);
        }

        public void Update(EventInvitation invitation)
        {
            _context.EventInvitations.Update(invitation);
        }
    }
}

// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/WeddingEventRepository.cs
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class WeddingEventRepository : IWeddingEventRepository
    {
        private readonly ApplicationDbContext _context;

        public WeddingEventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WeddingEvent weddingEvent, CancellationToken cancellationToken = default)
        {
            await _context.WeddingEvents.AddAsync(weddingEvent, cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class WeddingPlannerRepository : IWeddingPlannerRepository
{
    private readonly ApplicationDbContext _context;

    public WeddingPlannerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<WeddingPlanner?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.WeddingPlanners.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}

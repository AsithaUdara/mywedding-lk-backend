using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class PlannerClientEventRepository : IPlannerClientEventRepository
{
    private readonly ApplicationDbContext _context;

    public PlannerClientEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PlannerClientEvent plannerClientEvent, CancellationToken cancellationToken = default) =>
        _context.PlannerClientEvents.AddAsync(plannerClientEvent, cancellationToken).AsTask();
}

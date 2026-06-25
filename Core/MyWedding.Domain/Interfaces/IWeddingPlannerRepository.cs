using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface IWeddingPlannerRepository
{
    Task<WeddingPlanner?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

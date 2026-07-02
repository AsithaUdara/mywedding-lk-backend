using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface IPlannerClientEventRepository
{
    Task AddAsync(PlannerClientEvent plannerClientEvent, CancellationToken cancellationToken = default);
}

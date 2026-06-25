using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorCategoryRepository
    {
        Task<IReadOnlyList<VendorCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    }
}

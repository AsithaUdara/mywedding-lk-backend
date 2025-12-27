using MyWedding.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorRepository
    {
        Task<Vendor?> GetByIdAsync(string vendorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);
        // We will add more complex search methods here later
    }
}

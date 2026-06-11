using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorRepository
    {
        Task<Vendor?> GetByIdAsync(string vendorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Vendor>> GetPendingVendorsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Vendor>> GetAdminVendorsAsync(
            VerificationStatus? status = null,
            CancellationToken cancellationToken = default);
        Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
        void Update(Vendor vendor);
        Task<bool> SetVerificationStatusAsync(
            string vendorId,
            VerificationStatus status,
            CancellationToken cancellationToken = default);
    }
}

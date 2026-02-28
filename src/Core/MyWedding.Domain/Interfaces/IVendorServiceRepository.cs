// File: src/Core/MyWedding.Domain/Interfaces/IVendorServiceRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorServiceRepository
    {
        Task<VendorService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<VendorService>> GetByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default);
        Task AddAsync(VendorService service, CancellationToken cancellationToken = default);
        void Update(VendorService service);
        void Delete(VendorService service);
    }
}

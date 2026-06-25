using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorBlockedDateRepository
    {
        Task<IReadOnlyList<VendorBlockedDate>> GetByVendorAndMonthAsync(
            string vendorId,
            int year,
            int month,
            CancellationToken cancellationToken = default);
        Task<VendorBlockedDate?> GetByVendorAndDateAsync(
            string vendorId,
            DateTime date,
            CancellationToken cancellationToken = default);
        Task AddAsync(VendorBlockedDate blockedDate, CancellationToken cancellationToken = default);
        Task DeleteAsync(VendorBlockedDate blockedDate, CancellationToken cancellationToken = default);
    }
}

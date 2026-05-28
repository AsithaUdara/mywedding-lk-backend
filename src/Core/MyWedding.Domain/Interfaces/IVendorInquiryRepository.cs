using MyWedding.Domain.Entities;
using MyWedding.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorInquiryRepository
    {
        Task AddAsync(VendorInquiry inquiry, CancellationToken cancellationToken = default);
        Task<IEnumerable<VendorInquiry>> GetByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VendorInquiryInboxRow>> GetInboxByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default);
        Task<VendorInquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<VendorInquiry?> GetByIdForVendorAsync(Guid id, string vendorId, CancellationToken cancellationToken = default);
    }
}

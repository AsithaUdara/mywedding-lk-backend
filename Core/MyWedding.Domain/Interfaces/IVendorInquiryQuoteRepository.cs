using MyWedding.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorInquiryQuoteRepository
    {
        Task AddAsync(VendorInquiryQuote quote, CancellationToken cancellationToken = default);
        Task<VendorInquiryQuote?> GetLatestByInquiryIdAsync(Guid inquiryId, CancellationToken cancellationToken = default);
    }
}

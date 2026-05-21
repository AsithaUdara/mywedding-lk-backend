using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Inquiries.Queries.GetVendorInquiries
{
    public class GetVendorInquiriesQueryHandler : IRequestHandler<GetVendorInquiriesQuery, IEnumerable<VendorInquiryDto>>
    {
        private readonly IVendorInquiryRepository _inquiryRepository;

        public GetVendorInquiriesQueryHandler(IVendorInquiryRepository inquiryRepository)
        {
            _inquiryRepository = inquiryRepository;
        }

        public async Task<IEnumerable<VendorInquiryDto>> Handle(GetVendorInquiriesQuery request, CancellationToken cancellationToken)
        {
            var inquiries = await _inquiryRepository.GetByVendorIdAsync(request.VendorId, cancellationToken);
            
            return inquiries.Select(i => new VendorInquiryDto
            {
                Id = i.Id,
                Message = i.Message,
                SenderEmail = i.SenderEmail,
                SenderId = i.SenderId,
                VendorId = i.VendorId,
                SentAt = i.SentAt,
                IsRead = i.IsRead
            });
        }
    }
}

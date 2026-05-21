using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Inquiries.Queries.GetVendorInquiries
{
    public class GetVendorInquiriesQuery : IRequest<IEnumerable<VendorInquiryDto>>
    {
        public required string VendorId { get; set; }
    }

    public class VendorInquiryDto
    {
        public Guid Id { get; set; }
        public required string Message { get; set; }
        public required string SenderEmail { get; set; }
        public required string SenderId { get; set; }
        public required string VendorId { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}

using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.MarkInquiryAsRead
{
    public class MarkInquiryAsReadCommand : IRequest<bool>
    {
        public Guid InquiryId { get; set; }
        public required string VendorId { get; set; }
    }
}

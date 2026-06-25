using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.SendInquiry
{
    public class SendInquiryCommand : IRequest<Guid>
    {
        public required string VendorId { get; set; }
        public required string Message { get; set; }
        public required string SenderId { get; set; }
        public required string SenderEmail { get; set; }
    }
}

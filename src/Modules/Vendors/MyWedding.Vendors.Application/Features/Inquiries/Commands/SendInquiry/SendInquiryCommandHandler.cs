using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.SendInquiry
{
    public class SendInquiryCommandHandler : IRequestHandler<SendInquiryCommand, Guid>
    {
        private readonly IVendorInquiryRepository _inquiryRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SendInquiryCommandHandler(
            IVendorInquiryRepository inquiryRepository, 
            IVendorRepository vendorRepository,
            IUnitOfWork unitOfWork)
        {
            _inquiryRepository = inquiryRepository;
            _vendorRepository = vendorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(SendInquiryCommand request, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {request.VendorId} not found.");
            }

            var inquiry = new VendorInquiry
            {
                Id = Guid.NewGuid(),
                Message = request.Message,
                SenderId = request.SenderId,
                SenderEmail = request.SenderEmail,
                VendorId = request.VendorId,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _inquiryRepository.AddAsync(inquiry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return inquiry.Id;
        }
    }
}

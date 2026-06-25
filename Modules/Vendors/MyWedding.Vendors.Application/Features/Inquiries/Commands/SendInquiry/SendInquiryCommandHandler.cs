using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.SendInquiry
{
    public class SendInquiryCommandHandler : IRequestHandler<SendInquiryCommand, Guid>
    {
        private readonly IVendorInquiryRepository _inquiryRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public SendInquiryCommandHandler(
            IVendorInquiryRepository inquiryRepository,
            IVendorRepository vendorRepository,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _inquiryRepository = inquiryRepository;
            _vendorRepository = vendorRepository;
            _notificationService = notificationService;
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

            await _notificationService.NotifyNewInquiryAsync(
                request.VendorId,
                new
                {
                    inquiryId = inquiry.Id,
                    senderEmail = request.SenderEmail,
                    message = "You received a new inquiry."
                },
                cancellationToken);

            return inquiry.Id;
        }
    }
}

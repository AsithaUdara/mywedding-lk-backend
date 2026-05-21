using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Inquiries.Commands.MarkInquiryAsRead
{
    public class MarkInquiryAsReadCommandHandler : IRequestHandler<MarkInquiryAsReadCommand, bool>
    {
        private readonly IVendorInquiryRepository _inquiryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkInquiryAsReadCommandHandler(
            IVendorInquiryRepository inquiryRepository, 
            IUnitOfWork unitOfWork)
        {
            _inquiryRepository = inquiryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(MarkInquiryAsReadCommand request, CancellationToken cancellationToken)
        {
            var inquiry = await _inquiryRepository.GetByIdAsync(request.InquiryId, cancellationToken);
            
            if (inquiry == null)
            {
                throw new NotFoundException(nameof(inquiry), request.InquiryId);
            }

            if (inquiry.VendorId != request.VendorId)
            {
                throw new ForbiddenAccessException();
            }

            inquiry.IsRead = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

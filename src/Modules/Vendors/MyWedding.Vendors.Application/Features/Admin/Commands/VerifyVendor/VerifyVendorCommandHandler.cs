using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor
{
    public class VerifyVendorCommandHandler : IRequestHandler<VerifyVendorCommand, bool>
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyVendorCommandHandler(IVendorRepository vendorRepository, IUnitOfWork unitOfWork)
        {
            _vendorRepository = vendorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(VerifyVendorCommand request, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
            if (vendor == null)
                throw new NotFoundException(nameof(vendor), request.VendorId);

            vendor.VerificationStatus = request.IsApproved
                ? VerificationStatus.Verified
                : VerificationStatus.Rejected;

            _vendorRepository.Update(vendor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor;

public class VerifyVendorCommandHandler : IRequestHandler<VerifyVendorCommand, bool>
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyVendorCommandHandler(
        IVendorRepository vendorRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _vendorRepository = vendorRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(VerifyVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
            throw new NotFoundException(nameof(vendor), request.VendorId);

        vendor.VerificationStatus = request.IsApproved
            ? VerificationStatus.Verified
            : VerificationStatus.Rejected;

        _vendorRepository.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!request.IsApproved)
        {
            var toEmail = vendor.User?.Email;
            if (!string.IsNullOrWhiteSpace(toEmail))
            {
                await _emailService.SendVendorRejectionAsync(
                    toEmail,
                    vendor.BusinessName,
                    cancellationToken);
            }
        }

        return true;
    }
}

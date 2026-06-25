using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<VerifyVendorCommandHandler> _logger;

    public VerifyVendorCommandHandler(
        IVendorRepository vendorRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyVendorCommandHandler> logger)
    {
        _vendorRepository = vendorRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(VerifyVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
            throw new NotFoundException(nameof(vendor), request.VendorId);

        var newStatus = request.IsApproved
            ? VerificationStatus.Verified
            : VerificationStatus.Rejected;

        var updated = await _vendorRepository.SetVerificationStatusAsync(
            request.VendorId,
            newStatus,
            cancellationToken);

        if (!updated)
            throw new NotFoundException(nameof(vendor), request.VendorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!request.IsApproved)
        {
            var toEmail = vendor.User?.Email;
            if (!string.IsNullOrWhiteSpace(toEmail))
            {
                try
                {
                    await _emailService.SendVendorRejectionAsync(
                        toEmail,
                        vendor.BusinessName,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Vendor {VendorId} rejected but rejection email to {Email} failed.",
                        request.VendorId,
                        toEmail);
                }
            }
        }

        return true;
    }
}

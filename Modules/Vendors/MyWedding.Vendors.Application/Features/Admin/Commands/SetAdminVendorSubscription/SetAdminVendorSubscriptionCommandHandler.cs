using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.SetAdminVendorSubscription;

public class SetAdminVendorSubscriptionCommandHandler
    : IRequestHandler<SetAdminVendorSubscriptionCommand, bool>
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IVendorSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetAdminVendorSubscriptionCommandHandler(
        IVendorRepository vendorRepository,
        IVendorSubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _vendorRepository = vendorRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        SetAdminVendorSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
            throw new NotFoundException(nameof(vendor), request.VendorId);

        await _subscriptionRepository.CancelActiveSubscriptionsAsync(request.VendorId, cancellationToken);

        await _subscriptionRepository.AddAsync(new VendorSubscription
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            Tier = request.Tier,
            Status = SubscriptionStatus.Active,
            MonthlyFee = request.MonthlyFee,
            StartsAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

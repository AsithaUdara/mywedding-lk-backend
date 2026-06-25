using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.MarkCommissionSettlementPaid;

public class MarkCommissionSettlementPaidCommandHandler
    : IRequestHandler<MarkCommissionSettlementPaidCommand, bool>
{
    private readonly ICommissionSettlementRepository _settlementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkCommissionSettlementPaidCommandHandler(
        ICommissionSettlementRepository settlementRepository,
        IUnitOfWork unitOfWork)
    {
        _settlementRepository = settlementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        MarkCommissionSettlementPaidCommand request,
        CancellationToken cancellationToken)
    {
        var settlement = await _settlementRepository.GetByIdAsync(request.SettlementId, cancellationToken);
        if (settlement is null)
            throw new NotFoundException(nameof(settlement), request.SettlementId);

        _settlementRepository.MarkAsSettled(settlement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

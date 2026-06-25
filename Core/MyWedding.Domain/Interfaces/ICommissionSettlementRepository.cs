using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface ICommissionSettlementRepository
{
    Task<IReadOnlyList<PayoutDueCommissionRecord>> GetPayoutDueAsync(
        CancellationToken cancellationToken = default);

    Task<CommissionSettlement?> GetByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);

    void MarkAsSettled(CommissionSettlement settlement);
}

public record PayoutDueCommissionRecord(
    Guid Id,
    Guid BookingId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal VendorNetAmount,
    DateTime CreatedAt,
    Guid ServiceId,
    Guid EventId);

using MyWedding.Domain.Entities;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Domain.Interfaces;

public interface ICommissionSettlementRepository
{
    Task<PagedResult<PayoutDueCommissionRecord>> GetPayoutDuePagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PayoutDueSummary> GetPayoutDueSummaryAsync(CancellationToken cancellationToken = default);

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

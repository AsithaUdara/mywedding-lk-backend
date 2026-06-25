using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class CommissionSettlementRepository : ICommissionSettlementRepository
{
    private readonly ApplicationDbContext _context;

    public CommissionSettlementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PayoutDueCommissionRecord>> GetPayoutDueAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.CommissionSettlements
            .AsNoTracking()
            .Where(c => !c.IsVendorPayoutSettled)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.BookingId,
                c.GrossAmount,
                c.CommissionAmount,
                c.VendorNetAmount,
                c.CreatedAt,
                ServiceId = c.Booking!.ServiceId,
                EventId = c.Booking!.EventId,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PayoutDueCommissionRecord(
                r.Id,
                r.BookingId,
                r.GrossAmount,
                r.CommissionAmount,
                r.VendorNetAmount,
                r.CreatedAt,
                r.ServiceId,
                r.EventId))
            .ToList();
    }

    public async Task<CommissionSettlement?> GetByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommissionSettlements
            .FirstOrDefaultAsync(c => c.Id == settlementId, cancellationToken);
    }

    public void MarkAsSettled(CommissionSettlement settlement)
    {
        settlement.IsVendorPayoutSettled = true;
        settlement.SettledAt = DateTime.UtcNow;
    }
}

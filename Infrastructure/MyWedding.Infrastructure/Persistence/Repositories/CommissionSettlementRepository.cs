using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class CommissionSettlementRepository : ICommissionSettlementRepository
{
    private readonly ApplicationDbContext _context;

    public CommissionSettlementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<CommissionSettlement> UnsettledQuery()
    {
        return _context.CommissionSettlements
            .AsNoTracking()
            .Where(c => !c.IsVendorPayoutSettled);
    }

    private static IQueryable<CommissionSettlement> ApplySearch(
        IQueryable<CommissionSettlement> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim().ToLower();
        return query.Where(c =>
            c.BookingId.ToString().ToLower().Contains(term) ||
            c.Id.ToString().ToLower().Contains(term) ||
            (c.Booking != null && c.Booking.EventId.ToString().ToLower().Contains(term)) ||
            (c.Booking != null && c.Booking.ServiceId.ToString().ToLower().Contains(term)));
    }

    public async Task<PagedResult<PayoutDueCommissionRecord>> GetPayoutDuePagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySearch(UnsettledQuery(), search)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var items = rows
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

        return new PagedResult<PayoutDueCommissionRecord>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PayoutDueSummary> GetPayoutDueSummaryAsync(CancellationToken cancellationToken = default)
    {
        var aggregate = await UnsettledQuery()
            .GroupBy(_ => 1)
            .Select(g => new PayoutDueSummary
            {
                Count = g.Count(),
                TotalGross = g.Sum(x => x.GrossAmount),
                TotalCommission = g.Sum(x => x.CommissionAmount),
                TotalVendorNet = g.Sum(x => x.VendorNetAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return aggregate ?? new PayoutDueSummary();
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

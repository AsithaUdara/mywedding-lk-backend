using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class VendorShortlistRepository : IVendorShortlistRepository
{
    private readonly ApplicationDbContext _context;

    public VendorShortlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VendorShortlistItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.VendorShortlistItems
            .Include(i => i.VendorService)
            .ThenInclude(s => s!.Vendor)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<VendorShortlistItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.VendorShortlistItems
            .AsNoTracking()
            .Include(i => i.VendorService)
            .ThenInclude(s => s!.Vendor)
            .Include(i => i.VendorBooking)
            .ThenInclude(b => b!.BookingContract)
            .Where(i => i.EventId == eventId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return _context.VendorShortlistItems
            .AnyAsync(i => i.VendorBookingId == bookingId, cancellationToken);
    }

    public async Task AddAsync(VendorShortlistItem item, CancellationToken cancellationToken = default)
    {
        await _context.VendorShortlistItems.AddAsync(item, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<VendorShortlistItem> items, CancellationToken cancellationToken = default)
    {
        await _context.VendorShortlistItems.AddRangeAsync(items, cancellationToken);
    }

    public void Update(VendorShortlistItem item)
    {
        _context.VendorShortlistItems.Update(item);
    }
}

using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface IVendorShortlistRepository
{
    Task<VendorShortlistItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorShortlistItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task AddAsync(VendorShortlistItem item, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<VendorShortlistItem> items, CancellationToken cancellationToken = default);
    void Update(VendorShortlistItem item);
}

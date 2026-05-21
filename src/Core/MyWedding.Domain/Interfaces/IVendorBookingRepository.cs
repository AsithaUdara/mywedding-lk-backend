using MyWedding.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorBookingRepository
    {
        Task AddAsync(VendorBooking booking, CancellationToken cancellationToken = default);
        Task<bool> HasBookingsAsync(Guid serviceId, CancellationToken cancellationToken = default);
        Task<System.Collections.Generic.IEnumerable<VendorBooking>> GetBookingsByVendorUserIdAsync(string vendorUserId, CancellationToken cancellationToken = default);
        Task<System.Collections.Generic.IEnumerable<VendorBooking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<VendorBooking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

using MyWedding.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorBookingRepository
    {
        Task AddAsync(VendorBooking booking, CancellationToken cancellationToken = default);
        Task<bool> HasBookingsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    }
}

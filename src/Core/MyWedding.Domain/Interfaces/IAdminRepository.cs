using MyWedding.Domain.ReadModels;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    /// <summary>
    /// Repository for platform-wide aggregated statistics used by the admin panel.
    /// </summary>
    public interface IAdminRepository
    {
        Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalVendorsAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalEventsAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalBookingsAsync(CancellationToken cancellationToken = default);
        Task<PlatformAnalyticsSnapshot> GetPlatformAnalyticsAsync(CancellationToken cancellationToken = default);
    }
}

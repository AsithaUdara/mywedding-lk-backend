using MyWedding.Domain.ReadModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorAnalyticsRepository
    {
        Task<IReadOnlyList<WeeklyViewCountPoint>> GetProfileViewsByWeekAsync(
            string vendorId,
            int weeks,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MonthlyCountPoint>> GetInquiriesByMonthAsync(
            string vendorId,
            int months,
            CancellationToken cancellationToken = default);

        Task<WinRateSummary> GetWinRateAsync(string vendorId, CancellationToken cancellationToken = default);
    }
}

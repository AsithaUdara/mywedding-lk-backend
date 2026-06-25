using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorProfileViewRepository
    {
        Task RecordViewAsync(string vendorId, CancellationToken cancellationToken = default);
    }
}

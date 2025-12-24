// File: src/Core/MyWedding.Domain/Interfaces/IWeddingEventRepository.cs
using MyWedding.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;

namespace MyWedding.Domain.Interfaces
{
    public interface IWeddingEventRepository
    {
        Task AddAsync(WeddingEvent weddingEvent, CancellationToken cancellationToken = default);
    }
}

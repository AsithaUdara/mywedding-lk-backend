using MyWedding.Domain.Entities;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
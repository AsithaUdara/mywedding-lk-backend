// File: src/Core/MyWedding.Domain/Interfaces/IFirebaseAuthService.cs
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task SetUserRoleAsync(string userId, string role);
    }
}

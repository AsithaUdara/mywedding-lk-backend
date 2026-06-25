using System;
using System.Threading.Tasks;

namespace MyWedding.SharedKernel.Security
{
    public interface IEventAuthorizationService
    {
        Task<bool> IsOrganizerAsync(Guid eventId, string userId);
    }
}

using System;

namespace MyWedding.SharedKernel.Security
{
    public interface IAuthorizeableRequest
    {
        Guid EventId { get; }
        string UserId { get; }
    }
}

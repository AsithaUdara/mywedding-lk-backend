using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLogItem item, CancellationToken cancellationToken = default);
        Task<IEnumerable<AuditLogItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    }
}

using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLogItem item, CancellationToken cancellationToken = default)
        {
            await _context.AuditLogItems.AddAsync(item, cancellationToken);
        }

        public async Task<IEnumerable<AuditLogItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogItems
                .Include(i => i.Actor)
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.TimestampUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        private IQueryable<AuditLogItem> BuildEventQuery(Guid eventId, string? search, string? actionType)
        {
            IQueryable<AuditLogItem> query = _context.AuditLogItems
                .Include(i => i.Actor)
                .AsNoTracking()
                .Where(i => i.EventId == eventId);

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(i => i.ActionType == actionType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.ActionType.ToLower().Contains(term) ||
                    i.Content.ToLower().Contains(term) ||
                    (i.MetadataJson != null && i.MetadataJson.ToLower().Contains(term)) ||
                    (i.Actor != null && i.Actor.Email != null && i.Actor.Email.ToLower().Contains(term)) ||
                    (i.Actor != null && i.Actor.FirstName.ToLower().Contains(term)) ||
                    (i.Actor != null && i.Actor.LastName.ToLower().Contains(term)) ||
                    i.Id.ToString().ToLower().Contains(term));
            }

            return query.OrderByDescending(i => i.TimestampUtc);
        }

        public async Task<PagedResult<AuditLogItem>> GetByEventIdPagedAsync(
            Guid eventId,
            string? search,
            string? actionType,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = BuildEventQuery(eventId, search, actionType);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLogItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<EventAuditStats> GetEventAuditStatsAsync(
            Guid eventId,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.AuditLogItems
                .AsNoTracking()
                .Where(i => i.EventId == eventId)
                .GroupBy(i => i.ActionType)
                .Select(g => new { ActionType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var actionTypeCounts = rows.ToDictionary(x => x.ActionType, x => x.Count);
            return new EventAuditStats
            {
                TotalEntries = actionTypeCounts.Values.Sum(),
                ActionTypeCounts = actionTypeCounts,
            };
        }
    }
}

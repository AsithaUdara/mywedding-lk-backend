using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAuditLogByEventId
{
    public class GetAuditLogByEventIdQueryHandler : IRequestHandler<GetAuditLogByEventIdQuery, IEnumerable<AuditLogItemDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public GetAuditLogByEventIdQueryHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<IEnumerable<AuditLogItemDto>> Handle(GetAuditLogByEventIdQuery request, CancellationToken cancellationToken)
        {
            var items = await _auditLogRepository.GetByEventIdAsync(request.EventId, cancellationToken);

            return items
                .OrderByDescending(i => i.TimestampUtc)
                .Select(i => new AuditLogItemDto(
                    i.Id,
                    i.ActionType,
                    i.Content,
                    i.MetadataJson,
                    i.TimestampUtc,
                    i.Actor!.Id,
                    i.Actor.FirstName,
                    i.Actor.LastName
                ));
        }
    }
}

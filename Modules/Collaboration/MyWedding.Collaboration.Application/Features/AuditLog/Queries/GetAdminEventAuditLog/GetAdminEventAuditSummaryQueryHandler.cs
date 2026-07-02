using MediatR;
using MyWedding.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

public class GetAdminEventAuditSummaryQueryHandler
    : IRequestHandler<GetAdminEventAuditSummaryQuery, AdminEventAuditSummaryDto?>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IWeddingEventRepository _eventRepository;

    public GetAdminEventAuditSummaryQueryHandler(
        IAuditLogRepository auditLogRepository,
        IWeddingEventRepository eventRepository)
    {
        _auditLogRepository = auditLogRepository;
        _eventRepository = eventRepository;
    }

    public async Task<AdminEventAuditSummaryDto?> Handle(
        GetAdminEventAuditSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            return null;
        }

        var stats = await _auditLogRepository.GetEventAuditStatsAsync(request.EventId, cancellationToken);

        return new AdminEventAuditSummaryDto
        {
            EventId = weddingEvent.Id,
            EventName = weddingEvent.EventName,
            EventDate = weddingEvent.EventDate,
            LifecycleStage = weddingEvent.EventLifecycleStage.ToString(),
            TotalEntries = stats.TotalEntries,
            ActionTypeCounts = stats.ActionTypeCounts.ToDictionary(x => x.Key, x => x.Value),
        };
    }
}

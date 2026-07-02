using MediatR;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

public class GetAdminEventAuditSummaryQuery : IRequest<AdminEventAuditSummaryDto?>
{
    public Guid EventId { get; init; }
}

public class AdminEventAuditSummaryDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string LifecycleStage { get; set; } = string.Empty;
    public int TotalEntries { get; set; }
    public Dictionary<string, int> ActionTypeCounts { get; set; } = new();
}

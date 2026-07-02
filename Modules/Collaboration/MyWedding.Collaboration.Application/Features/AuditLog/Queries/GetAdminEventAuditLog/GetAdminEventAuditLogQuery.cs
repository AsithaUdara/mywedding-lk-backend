using MediatR;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

public class GetAdminEventAuditLogQuery : IRequest<PagedResult<AuditLogEntryDto>>
{
    public Guid EventId { get; init; }
    public string? Search { get; set; }
    public string? ActionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AuditLogEntryDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorFirstName { get; set; } = string.Empty;
    public string ActorLastName { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
}

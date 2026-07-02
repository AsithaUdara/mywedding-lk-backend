using System;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAuditLogByEventId
{
    public record AuditLogItemDto(
        Guid Id,
        string ActionType,
        string Content,
        string? MetadataJson,
        DateTime TimestampUtc,
        string ActorId,
        string ActorFirstName,
        string ActorLastName,
        string ActorDisplayName
    );
}

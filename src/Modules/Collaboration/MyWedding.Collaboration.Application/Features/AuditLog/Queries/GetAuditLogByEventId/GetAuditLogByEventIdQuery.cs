using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAuditLogByEventId
{
    public class GetAuditLogByEventIdQuery : IRequest<IEnumerable<AuditLogItemDto>>
    {
        public Guid EventId { get; init; }
        public string? UserId { get; init; }
    }
}

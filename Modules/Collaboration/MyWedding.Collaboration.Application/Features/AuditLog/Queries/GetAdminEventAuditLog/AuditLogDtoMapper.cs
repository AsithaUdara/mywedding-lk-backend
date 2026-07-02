using MyWedding.Collaboration.Application.Features.AuditLog;
using MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAuditLogByEventId;
using MyWedding.Domain.Entities;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

internal static class AuditLogDtoMapper
{
    public static AuditLogItemDto Map(
        AuditLogItem item,
        IReadOnlyDictionary<string, EventOrganizer> organizers,
        WeddingEvent? weddingEvent)
    {
        organizers.TryGetValue(item.ActorId, out var organizer);
        var displayName = ActivityActorDisplayName.Resolve(
            item.Actor?.FirstName,
            item.Actor?.LastName,
            item.Actor?.Email,
            item.ActorId,
            weddingEvent?.ManagingPlannerId,
            organizer?.Role);

        return new AuditLogItemDto(
            item.Id,
            item.ActionType,
            item.Content,
            item.MetadataJson,
            item.TimestampUtc,
            item.Actor!.Id,
            item.Actor.FirstName,
            item.Actor.LastName,
            displayName);
    }

    public static Dictionary<string, EventOrganizer> IndexOrganizers(IEnumerable<EventOrganizer> organizers)
    {
        return organizers
            .GroupBy(o => o.UserId)
            .ToDictionary(g => g.Key, g => g.First());
    }
}

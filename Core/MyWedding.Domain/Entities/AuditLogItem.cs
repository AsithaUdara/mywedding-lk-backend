using System;

namespace MyWedding.Domain.Entities
{
    public class AuditLogItem
    {
        private AuditLogItem() { }

        public AuditLogItem(
            Guid id,
            Guid eventId,
            string actorId,
            string actionType,
            string content,
            string? metadataJson,
            DateTime timestampUtc)
        {
            Id = id;
            EventId = eventId;
            ActorId = actorId;
            ActionType = actionType;
            Content = content;
            MetadataJson = metadataJson;
            TimestampUtc = timestampUtc;
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public WeddingEvent? WeddingEvent { get; private set; }
        public string ActorId { get; private set; } = string.Empty;
        public User? Actor { get; private set; }
        public string ActionType { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public string? MetadataJson { get; private set; }
        public DateTime TimestampUtc { get; private set; }
    }
}

// File: src/Core/MyWedding.Domain/Entities/ActivityFeedItem.cs
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class ActivityFeedItem
    {
        public Guid Id { get; set; }
        public ActivityType ItemType { get; set; }
        public required string Content { get; set; } // The message content
        public DateTime CreatedAt { get; set; }

        // Foreign Key to the Event this activity belongs to
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }
        
        // Foreign Key to the User who generated this activity
        public required string UserId { get; set; }
        public User? User { get; set; }
    }
}

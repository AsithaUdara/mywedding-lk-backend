// File: src/Core/MyWedding.Domain/Entities/Conversation.cs
using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public required string Name { get; set; } // e.g., "# general"

        // Foreign Key to the WeddingEvent this conversation belongs to
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property: A conversation has a collection of messages
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

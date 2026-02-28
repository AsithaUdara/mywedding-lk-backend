// File: src/Core/MyWedding.Domain/Entities/Poll.cs
using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class Poll
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Foreign Keys
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        public required string CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // Navigation Properties
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}

// File: src/Core/MyWedding.Domain/Entities/WeddingEvent.cs
using System;

namespace MyWedding.Domain.Entities
{
    public class WeddingEvent
    {
        public Guid Id { get; set; } // A unique ID for the event itself
        public required string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public decimal TotalBudget { get; set; }
        
        // --- NEW PROPERTY ---
        // This will store a JSON string of the user's style choices,
        // e.g., { "style": "modern", "photography": "candid" }
        public string? StylePreferences { get; set; }

        // Foreign Key to the User who created the event
        public required string CreatedById { get; set; }
        public User? CreatedBy { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

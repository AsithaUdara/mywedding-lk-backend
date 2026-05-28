// File: src/Core/MyWedding.Domain/Entities/WeddingEvent.cs
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class WeddingEvent
    {
        public Guid Id { get; set; } // A unique ID for the event itself
        public required string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public decimal TotalBudget { get; set; }
        public string? ManagingPlannerId { get; set; }
        public WeddingPlanner? ManagingPlanner { get; set; }
        public EventLifecycleStage EventLifecycleStage { get; set; } = EventLifecycleStage.Lead;
        
        // Foreign Key to the User who created the event
        public required string CreatedById { get; set; }
        public User? CreatedBy { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

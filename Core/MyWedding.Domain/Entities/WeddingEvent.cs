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
        public TaskPlanPhase TaskPlanPhase { get; set; } = TaskPlanPhase.None;

        // Couple requirements (discovery brief) — captured before full Gantt generation
        public int? EstimatedGuestCount { get; set; }
        public int? GuestCountMax { get; set; }
        public string? WeddingStyle { get; set; }
        public string? VenuePreference { get; set; }
        public string? MustHavesNotes { get; set; }
        public string? ServicesAlreadyBooked { get; set; }
        public string? CulturalOrReligiousNotes { get; set; }
        public DateTime? BriefCompletedAt { get; set; }

        // Foreign Key to the User who created the event
        public required string CreatedById { get; set; }
        public User? CreatedBy { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static WeddingEvent Create(string eventName, DateTime eventDate, string createdById)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name is required.", nameof(eventName));
            }

            if (string.IsNullOrWhiteSpace(createdById))
            {
                throw new ArgumentException("Creator user id is required.", nameof(createdById));
            }

            if (eventDate <= DateTime.UtcNow)
            {
                throw new ArgumentException("Event date must be in the future.", nameof(eventDate));
            }

            var now = DateTime.UtcNow;
            return new WeddingEvent
            {
                Id = Guid.NewGuid(),
                EventName = eventName.Trim(),
                EventDate = eventDate,
                CreatedById = createdById,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }
}

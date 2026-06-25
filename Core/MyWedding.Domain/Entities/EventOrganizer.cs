// File: src/Core/MyWedding.Domain/Entities/EventOrganizer.cs
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class EventOrganizer
    {
        // Composite Primary Key - Part 1: Foreign Key to WeddingEvent
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        // Composite Primary Key - Part 2: Foreign Key to User
        public required string UserId { get; set; }
        public User? User { get; set; }

        // Additional properties for this relationship
        public OrganizerRole Role { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}

// File: src/Core/MyWedding.Domain/Entities/EventTask.cs
using System;
using System.Collections.Generic;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class EventTask
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public MyWedding.Domain.Enums.TaskStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? DependsOnTaskId { get; set; }
        public EventTask? DependsOnTask { get; set; }
        public ICollection<EventTask> DependentTasks { get; set; } = new List<EventTask>();
        public string? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        
        // Foreign Key to the WeddingEvent this task belongs to
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
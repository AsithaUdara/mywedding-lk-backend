using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Entities
{
    public class PlannerClientEvent
    {
        public Guid Id { get; set; }

        public required string PlannerId { get; set; }
        public WeddingPlanner? Planner { get; set; }

        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        // Usually couple account that owns or represents client
        public required string ClientUserId { get; set; }
        public User? ClientUser { get; set; }

        public PlannerClientEventStatus Status { get; set; } = PlannerClientEventStatus.Active;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

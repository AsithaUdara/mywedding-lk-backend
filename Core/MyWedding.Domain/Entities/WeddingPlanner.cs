using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class WeddingPlanner
    {
        // PK + FK to User
        public required string UserId { get; set; }
        public User? User { get; set; }

        public required string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? ContactPhone { get; set; }
        public string? City { get; set; }
        /// <summary>Cloudinary URL for agency white-label logo (Planner Pro).</summary>
        public string? AgencyLogoUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<PlannerClientEvent> ClientEvents { get; set; } = new List<PlannerClientEvent>();
        public ICollection<PlannerSubscription> Subscriptions { get; set; } = new List<PlannerSubscription>();
    }
}

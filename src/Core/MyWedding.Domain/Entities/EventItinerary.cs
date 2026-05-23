using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class EventItinerary
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }

        public bool IsAiGenerated { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<EventItineraryItem> Items { get; set; } = new List<EventItineraryItem>();
    }
}

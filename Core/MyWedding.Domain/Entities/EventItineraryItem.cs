using System;

namespace MyWedding.Domain.Entities
{
    public class EventItineraryItem
    {
        public Guid Id { get; set; }
        public Guid ItineraryId { get; set; }
        public EventItinerary? Itinerary { get; set; }

        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int SortOrder { get; set; }
    }
}

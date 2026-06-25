using MediatR;

namespace MyWedding.Events.Application.Features.EventBrief.UpdateEventBrief;

public class UpdateEventBriefCommand : IRequest<EventBriefDto>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
    public int? EstimatedGuestCount { get; set; }
    public int? GuestCountMax { get; set; }
    public string? WeddingStyle { get; set; }
    public string? VenuePreference { get; set; }
    public string? MustHavesNotes { get; set; }
    public string? ServicesAlreadyBooked { get; set; }
    public string? CulturalOrReligiousNotes { get; set; }
    public bool MarkBriefComplete { get; set; }
}

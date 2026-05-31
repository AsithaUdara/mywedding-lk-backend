namespace MyWedding.Events.Application.Features.Events.Queries.GetEventById;

public record EventDto(
    Guid Id,
    string EventName,
    DateTime EventDate,
    string CreatedById,
    decimal TotalBudget,
    bool CanBook = false,
    EventPlannerBrandingDto? PlannerBranding = null);

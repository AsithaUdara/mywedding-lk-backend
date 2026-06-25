namespace MyWedding.Events.Application.Features.Events.Queries.GetEventById;

public record EventPlannerBrandingDto(
    string BusinessName,
    string DisplayName,
    string? AgencyLogoUrl,
    bool IsWhiteLabeled);

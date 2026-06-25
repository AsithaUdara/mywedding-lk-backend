using MediatR;

namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetChecklistPlanPreview;

public class GetChecklistPlanPreviewQuery : IRequest<ChecklistPlanPreviewDto>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
}

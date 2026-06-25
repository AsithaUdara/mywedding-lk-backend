using MediatR;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GeneratePersonalizedChecklistPlan;

public class GeneratePersonalizedChecklistPlanCommand : IRequest<PersonalizedChecklistPlanDto>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
    public string? MeetingNotesOrTranscript { get; set; }
}

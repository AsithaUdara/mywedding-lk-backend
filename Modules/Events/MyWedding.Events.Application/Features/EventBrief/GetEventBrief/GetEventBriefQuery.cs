using MediatR;

namespace MyWedding.Events.Application.Features.EventBrief.GetEventBrief;

public class GetEventBriefQuery : IRequest<EventBriefDto>
{
    public Guid EventId { get; init; }
    public required string UserId { get; init; }
}

using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Events.Application.Features.Events.Commands.UpdateEventLifecycleStage
{
    public class UpdateEventLifecycleStageCommand : IRequest
    {
        public Guid EventId { get; init; }
        public EventLifecycleStage NewStage { get; init; }
    }
}

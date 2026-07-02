using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.Events.Commands.UpdateEventLifecycleStage
{
    public class UpdateEventLifecycleStageCommandValidator : AbstractValidator<UpdateEventLifecycleStageCommand>
    {
        public UpdateEventLifecycleStageCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.NewStage).IsInEnum();
        }
    }
}

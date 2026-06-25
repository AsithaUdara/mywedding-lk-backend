using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.RealignEventTaskSchedule;

public class RealignEventTaskScheduleCommandValidator : AbstractValidator<RealignEventTaskScheduleCommand>
{
    public RealignEventTaskScheduleCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId().When(v => v.UserId is not null);
    }
}

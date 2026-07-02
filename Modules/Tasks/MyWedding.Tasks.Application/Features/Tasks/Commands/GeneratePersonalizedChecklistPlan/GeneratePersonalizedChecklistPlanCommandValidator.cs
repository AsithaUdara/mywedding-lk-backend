using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GeneratePersonalizedChecklistPlan;

public class GeneratePersonalizedChecklistPlanCommandValidator : AbstractValidator<GeneratePersonalizedChecklistPlanCommand>
{
    public GeneratePersonalizedChecklistPlanCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId();
        RuleFor(v => v.MeetingNotesOrTranscript).MaximumLength(10000).When(v => !string.IsNullOrWhiteSpace(v.MeetingNotesOrTranscript));
    }
}

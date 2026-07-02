using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.ApplyPersonalizedChecklist;

public class ApplyPersonalizedChecklistCommandValidator : AbstractValidator<ApplyPersonalizedChecklistCommand>
{
    public ApplyPersonalizedChecklistCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId();
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class SavePlannerTaskTemplateFromEventCommandValidator : AbstractValidator<SavePlannerTaskTemplateFromEventCommand>
{
    public SavePlannerTaskTemplateFromEventCommandValidator()
    {
        RuleFor(v => v.PlannerId).ValidUserId();
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.Description).MaximumLength(1000).When(v => !string.IsNullOrWhiteSpace(v.Description));
    }
}

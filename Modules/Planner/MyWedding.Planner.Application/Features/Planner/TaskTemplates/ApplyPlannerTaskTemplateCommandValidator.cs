using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class ApplyPlannerTaskTemplateCommandValidator : AbstractValidator<ApplyPlannerTaskTemplateCommand>
{
    public ApplyPlannerTaskTemplateCommandValidator()
    {
        RuleFor(v => v.PlannerId).ValidUserId();
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.TemplateId).NotEmpty();
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class DeletePlannerTaskTemplateCommandValidator : AbstractValidator<DeletePlannerTaskTemplateCommand>
{
    public DeletePlannerTaskTemplateCommandValidator()
    {
        RuleFor(v => v.PlannerId).ValidUserId();
        RuleFor(v => v.TemplateId).NotEmpty();
    }
}

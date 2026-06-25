using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

public class GenerateTaskTemplateCommandValidator : AbstractValidator<GenerateTaskTemplateCommand>
{
    public GenerateTaskTemplateCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId();
    }
}

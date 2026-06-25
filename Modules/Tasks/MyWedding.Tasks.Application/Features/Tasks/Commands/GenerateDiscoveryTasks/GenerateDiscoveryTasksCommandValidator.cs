using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateDiscoveryTasks;

public class GenerateDiscoveryTasksCommandValidator : AbstractValidator<GenerateDiscoveryTasksCommand>
{
    public GenerateDiscoveryTasksCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId();
    }
}

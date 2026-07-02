using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.AssignTask;

public class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(v => v.TaskId).NotEmpty();
        RuleFor(v => v.UserId).ValidUserId();
        RuleFor(v => v.AssignedToUserId).ValidUserId().When(v => v.AssignedToUserId is not null);
    }
}

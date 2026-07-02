using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
    {
        public UpdateTaskStatusCommandValidator()
        {
            RuleFor(v => v.TaskId).NotEmpty();
            RuleFor(v => v.UserId).ValidUserId();
            RuleFor(v => v.NewStatus).IsInEnum();
        }
    }
}

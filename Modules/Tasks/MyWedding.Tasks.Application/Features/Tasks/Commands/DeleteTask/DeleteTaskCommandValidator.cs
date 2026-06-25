using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.DeleteTask
{
    public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
    {
        public DeleteTaskCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.TaskId).NotEmpty();
            RuleFor(v => v.UserId).ValidUserId().When(v => v.UserId is not null);
        }
    }
}

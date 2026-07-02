using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            RuleFor(v => v.TaskId).NotEmpty();
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.Title).ValidTitle();
            RuleFor(v => v.UserId).ValidUserId();
        }
    }
}

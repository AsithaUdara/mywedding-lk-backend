using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTaskSchedule
{
    public class UpdateTaskScheduleCommandValidator : AbstractValidator<UpdateTaskScheduleCommand>
    {
        public UpdateTaskScheduleCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.TaskId).NotEmpty();
            RuleFor(v => v.UserId).ValidUserId().When(v => v.UserId is not null);
            RuleFor(v => v.DueDate)
                .GreaterThan(v => v.StartDate!.Value)
                .When(v => v.StartDate.HasValue && v.DueDate.HasValue);
            RuleFor(v => v.DependsOnTaskId).NotEmpty().When(v => v.UpdateDependency && v.DependsOnTaskId.HasValue);
        }
    }
}

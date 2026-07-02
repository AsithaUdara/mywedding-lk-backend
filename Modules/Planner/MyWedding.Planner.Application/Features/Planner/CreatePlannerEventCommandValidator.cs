using FluentValidation;
using MyWedding.Planner.Application.Features.Planner;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Planner.Application.Features.Planner
{
    public class CreatePlannerEventCommandValidator : AbstractValidator<CreatePlannerEventCommand>
    {
        public CreatePlannerEventCommandValidator()
        {
            RuleFor(v => v.PlannerId).ValidUserId();
            RuleFor(v => v.EventName).NotEmpty().MaximumLength(100);
            RuleFor(v => v.EventDate).GreaterThan(DateTime.UtcNow);
            RuleFor(v => v.TotalBudget).GreaterThanOrEqualTo(0);
        }
    }
}

using FluentValidation;
using System;

namespace MyWedding.Events.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
    {
        public CreateEventCommandValidator()
        {
            RuleFor(v => v.EventName)
                .NotEmpty().WithMessage("Event Name is required.")
                .MaximumLength(100).WithMessage("Event Name must not exceed 100 characters.");

            RuleFor(v => v.EventDate)
                .NotEmpty().WithMessage("Event Date is required.")
                .Must(BeAFutureDate).WithMessage("Event Date must be in the future.");

            RuleFor(v => v.UserId)
                .NotEmpty().WithMessage("User ID is required.");
        }

        private bool BeAFutureDate(DateTime date)
        {
            return date > DateTime.UtcNow;
        }
    }
}

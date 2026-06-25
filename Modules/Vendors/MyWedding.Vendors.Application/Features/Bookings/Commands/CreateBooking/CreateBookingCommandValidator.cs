using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.ServiceId).NotEmpty();
            RuleFor(v => v.UserId).ValidUserId();
            RuleFor(v => v.FinalAmount).GreaterThan(0);
            RuleFor(v => v.ServiceDate).GreaterThan(DateTime.UtcNow);
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.ApproveBooking;

public class ApproveBookingCommandValidator : AbstractValidator<ApproveBookingCommand>
{
    public ApproveBookingCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.UserId).ValidUserId();
    }
}

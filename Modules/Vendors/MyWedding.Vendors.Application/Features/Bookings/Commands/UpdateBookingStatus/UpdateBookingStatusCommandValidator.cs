using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.UpdateBookingStatus
{
    public class UpdateBookingStatusCommandValidator : AbstractValidator<UpdateBookingStatusCommand>
    {
        public UpdateBookingStatusCommandValidator()
        {
            RuleFor(v => v.BookingId).NotEmpty();
            RuleFor(v => v.VendorUserId).ValidUserId();
            RuleFor(v => v.NewStatus).IsInEnum();
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.AcceptVendorBooking;

public class AcceptVendorBookingCommandValidator : AbstractValidator<AcceptVendorBookingCommand>
{
    public AcceptVendorBookingCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.VendorUserId).ValidUserId();
    }
}

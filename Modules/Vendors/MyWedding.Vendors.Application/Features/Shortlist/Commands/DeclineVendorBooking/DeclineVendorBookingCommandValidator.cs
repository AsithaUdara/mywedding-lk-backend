using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.DeclineVendorBooking;

public class DeclineVendorBookingCommandValidator : AbstractValidator<DeclineVendorBookingCommand>
{
    public DeclineVendorBookingCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.VendorUserId).ValidUserId();
    }
}

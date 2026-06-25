using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.RequestVendorBooking;

public class RequestVendorBookingCommandValidator : AbstractValidator<RequestVendorBookingCommand>
{
    public RequestVendorBookingCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.ShortlistItemId).NotEmpty();
        RuleFor(v => v.UserId).ValidUserId();
    }
}

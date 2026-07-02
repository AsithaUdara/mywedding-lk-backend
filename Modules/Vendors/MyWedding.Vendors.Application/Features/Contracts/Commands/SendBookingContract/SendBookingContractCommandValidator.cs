using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SendBookingContract;

public class SendBookingContractCommandValidator : AbstractValidator<SendBookingContractCommand>
{
    public SendBookingContractCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.VendorUserId).ValidUserId();
    }
}

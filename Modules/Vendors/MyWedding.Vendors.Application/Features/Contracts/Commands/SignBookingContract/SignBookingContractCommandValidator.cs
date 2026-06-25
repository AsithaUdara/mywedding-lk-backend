using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SignBookingContract;

public class SignBookingContractCommandValidator : AbstractValidator<SignBookingContractCommand>
{
    public SignBookingContractCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.UserId).ValidUserId();
        RuleFor(v => v.SignerName).NotEmpty().MaximumLength(200);
        RuleFor(v => v.ClientIpAddress).NotEmpty().MaximumLength(45);
    }
}

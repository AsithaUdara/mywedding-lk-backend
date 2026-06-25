using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.UploadBookingContract;

public class UploadBookingContractCommandValidator : AbstractValidator<UploadBookingContractCommand>
{
    public UploadBookingContractCommandValidator()
    {
        RuleFor(v => v.BookingId).NotEmpty();
        RuleFor(v => v.VendorUserId).ValidUserId();
        RuleFor(v => v)
            .Must(v => v.GenerateStandardContract || (v.PdfBytes is { Length: > 0 }))
            .WithMessage("Either provide contract PDF bytes or request a standard contract.");
    }
}

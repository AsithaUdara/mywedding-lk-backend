using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.ApproveVendorFromShortlist;

public class ApproveVendorFromShortlistCommandValidator : AbstractValidator<ApproveVendorFromShortlistCommand>
{
    public ApproveVendorFromShortlistCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.ShortlistItemId).NotEmpty();
        RuleFor(v => v.ClientUserId).ValidUserId();
    }
}

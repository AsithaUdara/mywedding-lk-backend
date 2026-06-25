using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.SendShortlistToClient;

public class SendShortlistToClientCommandValidator : AbstractValidator<SendShortlistToClientCommand>
{
    public SendShortlistToClientCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.PlannerId).ValidUserId();
        RuleForEach(v => v.ItemIds).NotEmpty().When(v => v.ItemIds is not null);
    }
}

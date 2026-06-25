using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.Invitations.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.InvitedById).ValidUserId();
    }
}

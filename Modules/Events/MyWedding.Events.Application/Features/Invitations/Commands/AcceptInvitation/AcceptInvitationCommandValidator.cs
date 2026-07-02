using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.Invitations.Commands.AcceptInvitation
{
    public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
    {
        public AcceptInvitationCommandValidator()
        {
            RuleFor(v => v.Token).NotEmpty();
            RuleFor(v => v.UserId).ValidUserId();
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Identity.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserCommandValidator : AbstractValidator<SyncUserCommand>
    {
        public SyncUserCommandValidator()
        {
            RuleFor(v => v.FirebaseUid).ValidUserId();
            RuleFor(v => v.Email).NotEmpty().EmailAddress();
        }
    }
}

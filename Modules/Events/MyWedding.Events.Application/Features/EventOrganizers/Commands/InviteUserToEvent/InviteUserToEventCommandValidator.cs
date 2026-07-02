using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.EventOrganizers.Commands.InviteUserToEvent
{
    public class InviteUserToEventCommandValidator : AbstractValidator<InviteUserToEventCommand>
    {
        public InviteUserToEventCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.InviteeEmail).NotEmpty().EmailAddress();
            RuleFor(v => v.InviterUserId).ValidUserId();
        }
    }
}

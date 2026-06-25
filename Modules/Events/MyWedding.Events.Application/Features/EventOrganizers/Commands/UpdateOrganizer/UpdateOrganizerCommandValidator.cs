using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.EventOrganizers.Commands.UpdateOrganizer
{
    public class UpdateOrganizerCommandValidator : AbstractValidator<UpdateOrganizerCommand>
    {
        public UpdateOrganizerCommandValidator()
        {
            RuleFor(v => v.EventId).ValidEventId();
            RuleFor(v => v.TargetUserId).ValidUserId();
            RuleFor(v => v.RequestingUserId).ValidUserId();
        }
    }
}

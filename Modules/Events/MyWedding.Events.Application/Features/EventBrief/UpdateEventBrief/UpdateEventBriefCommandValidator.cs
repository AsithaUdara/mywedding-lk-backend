using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Events.Application.Features.EventBrief.UpdateEventBrief;

public class UpdateEventBriefCommandValidator : AbstractValidator<UpdateEventBriefCommand>
{
    public UpdateEventBriefCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.UserId).ValidUserId();
        RuleFor(v => v.EstimatedGuestCount).GreaterThan(0).When(v => v.EstimatedGuestCount.HasValue);
        RuleFor(v => v.GuestCountMax).GreaterThan(0).When(v => v.GuestCountMax.HasValue);
        RuleFor(v => v.WeddingStyle).MaximumLength(200).When(v => !string.IsNullOrWhiteSpace(v.WeddingStyle));
        RuleFor(v => v.VenuePreference).MaximumLength(200).When(v => !string.IsNullOrWhiteSpace(v.VenuePreference));
        RuleFor(v => v.MustHavesNotes).MaximumLength(2000).When(v => !string.IsNullOrWhiteSpace(v.MustHavesNotes));
        RuleFor(v => v.ServicesAlreadyBooked).MaximumLength(2000).When(v => !string.IsNullOrWhiteSpace(v.ServicesAlreadyBooked));
        RuleFor(v => v.CulturalOrReligiousNotes).MaximumLength(2000).When(v => !string.IsNullOrWhiteSpace(v.CulturalOrReligiousNotes));
    }
}

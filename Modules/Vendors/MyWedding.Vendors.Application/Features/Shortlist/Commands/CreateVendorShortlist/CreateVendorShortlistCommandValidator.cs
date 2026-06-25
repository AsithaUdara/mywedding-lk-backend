using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.CreateVendorShortlist;

public class CreateVendorShortlistCommandValidator : AbstractValidator<CreateVendorShortlistCommand>
{
    public CreateVendorShortlistCommandValidator()
    {
        RuleFor(v => v.EventId).ValidEventId();
        RuleFor(v => v.PlannerId).ValidUserId();
        RuleFor(v => v.Items).NotEmpty();
        RuleForEach(v => v.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VendorServiceId).NotEmpty();
            item.RuleFor(i => i.ProposedAmount).GreaterThan(0);
            item.RuleFor(i => i.ServiceDate).GreaterThan(DateTime.UtcNow).When(i => i.ServiceDate.HasValue);
            item.RuleFor(i => i.CategoryLabel).MaximumLength(100).When(i => !string.IsNullOrWhiteSpace(i.CategoryLabel));
            item.RuleFor(i => i.PlannerNotes).MaximumLength(2000).When(i => !string.IsNullOrWhiteSpace(i.PlannerNotes));
        });
    }
}

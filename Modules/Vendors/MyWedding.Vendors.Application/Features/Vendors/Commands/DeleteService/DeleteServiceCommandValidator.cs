using FluentValidation;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.DeleteService
{
    public class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
    {
        public DeleteServiceCommandValidator()
        {
            RuleFor(v => v.Id).NotEmpty();
        }
    }
}

using FluentValidation;
using MyWedding.SharedKernel.Validation;

namespace MyWedding.Collaboration.Application.Features.Messages.Commands.PostMessage
{
    public class PostMessageCommandValidator : AbstractValidator<PostMessageCommand>
    {
        public PostMessageCommandValidator()
        {
            RuleFor(v => v.ConversationId).NotEmpty();
            RuleFor(v => v.SenderId).ValidUserId();
            RuleFor(v => v.Content).NotEmpty().MaximumLength(4000);
        }
    }
}

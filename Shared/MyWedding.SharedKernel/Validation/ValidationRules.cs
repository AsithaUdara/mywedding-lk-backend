using FluentValidation;

namespace MyWedding.SharedKernel.Validation
{
    public static class ValidationRules
    {
        public static IRuleBuilderOptions<T, Guid> ValidEventId<T>(this IRuleBuilder<T, Guid> ruleBuilder) =>
            ruleBuilder.NotEmpty().WithMessage("Event ID is required.");

        public static IRuleBuilderOptions<T, string?> ValidUserId<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
            ruleBuilder.NotEmpty().WithMessage("User ID is required.");

        public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> ruleBuilder) =>
            ruleBuilder.NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}

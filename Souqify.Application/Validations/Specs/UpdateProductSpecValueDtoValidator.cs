using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    // CAT-4 AC 3: one answer's shape. No database here: "is this key on the form?" and "does this slug exist?" are the service's job.
    public class UpdateProductSpecValueDtoValidator : AbstractValidator<UpdateProductSpecValueDto>
    {
        public UpdateProductSpecValueDtoValidator()
        {
            RuleFor(v => v.Key)
                .NotEmpty()
                .MaximumLength(50);     // same as the column (SpecAttributeConfiguration)

            // Exactly one answer field is sent (not null). The page sends the other 4 as null.
            RuleFor(v => v)
                .Must(v => CountSentFields(v) == 1)
                .WithName("Value")
                .WithMessage(v => $"'{v.Key}' must have exactly one of: option, options, number, bool, text.");

            // The sent field can't be empty. Without this, "" would pass the count above, and the database's
            // check constraint (num_nonnulls) would count it as filled too.
            RuleFor(v => v.Option)
                .NotEmpty()
                .MaximumLength(50)      // same as SpecOptions.Slug
                .When(v => v.Option != null);

            RuleFor(v => v.Text)
                .NotEmpty()
                .MaximumLength(200)     // same as ProductSpecValues.TextValue
                .When(v => v.Text != null);

            RuleFor(v => v.Number)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(12, 3, true)    // same as ProductSpecValues.NumberValue: numeric(12,3)
                .When(v => v.Number != null);

            RuleFor(v => v.Options)
                .NotEmpty()
                    .WithMessage(v => $"'{v.Key}': options can't be an empty list.")
                .Must(options => options!.Distinct().Count() == options!.Count)
                    .WithMessage(v => $"'{v.Key}': the same choice is sent twice in options.")   // the unique index would refuse it with a 500
                .When(v => v.Options != null);

            RuleForEach(v => v.Options)
                .NotEmpty()
                .MaximumLength(50)      // same as SpecOptions.Slug
                .When(v => v.Options != null);
        }

        private static int CountSentFields(UpdateProductSpecValueDto value)
        {
            int sent = 0;

            if (value.Option != null) sent++;
            if (value.Options != null) sent++;
            if (value.Number != null) sent++;
            if (value.Bool != null) sent++;
            if (value.Text != null) sent++;

            return sent;
        }
    }
}

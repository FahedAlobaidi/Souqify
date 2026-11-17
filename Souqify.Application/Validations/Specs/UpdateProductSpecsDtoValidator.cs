using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    // CAT-4 AC 3: the PUT body's shape. Each answer is checked by UpdateProductSpecValueDtoValidator.
    public class UpdateProductSpecsDtoValidator : AbstractValidator<UpdateProductSpecsDto>
    {
        public UpdateProductSpecsDtoValidator()
        {
            // xmin is never 0 for a real row, so 0 means the page forgot to send it.
            // Without this rule it would reach the RowVersion comparison and come back as a confusing 409.
            RuleFor(s => s.RowVersion)
                .GreaterThan(0u)
                    .WithMessage("rowVersion is missing. Send the rowVersion from GET /api/admin/products/{productId}/specs.");

            RuleFor(s => s.Values)
                .NotNull();

            // One item per question. A MultiOption question sends all its choices together in options,
            // so a key never needs to come twice.
            RuleFor(s => s.Values)
                .Must(values => FindRepeatedKeys(values).Count == 0)
                    .WithMessage(s => $"Each key can only be sent once. Repeated: {string.Join(", ", FindRepeatedKeys(s.Values))}.")
                .When(s => s.Values != null);

            RuleForEach(s => s.Values)
                .NotNull()
                .SetValidator(new UpdateProductSpecValueDtoValidator());
        }

        private static List<string> FindRepeatedKeys(List<UpdateProductSpecValueDto> values)
        {
            List<string> repeated = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            foreach (var value in values)
            {
                if (value == null || value.Key == null)
                    continue;   // the item rules report these

                // Add returns false when the key was already seen.
                if (!seen.Add(value.Key) && !repeated.Contains(value.Key))
                    repeated.Add(value.Key);
            }

            return repeated;
        }
    }
}

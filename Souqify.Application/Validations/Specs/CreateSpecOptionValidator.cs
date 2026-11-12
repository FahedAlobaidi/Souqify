
using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    public class CreateSpecOptionValidator : AbstractValidator<CreateSpecOptionDto>
    {
        public CreateSpecOptionValidator()
        {
            RuleFor(so => so.Value)
                .NotEmpty()
                .MaximumLength(50);     // same as the column (SpecOptionConfiguration)

            RuleFor(so => so.DisplayOrder)
                .GreaterThanOrEqualTo(0);
        }
    }
}

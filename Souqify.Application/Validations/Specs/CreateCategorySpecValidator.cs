
using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    public class CreateCategorySpecValidator : AbstractValidator<CreateCategorySpecDto>
    {
        public CreateCategorySpecValidator()
        {
            RuleFor(cs => cs.SpecAttributeId)
                .NotEmpty();            // a missing id arrives as 00000000-0000-0000-0000-000000000000

            RuleFor(cs => cs.DisplayOrder)
                .GreaterThanOrEqualTo(0);
        }
    }
}

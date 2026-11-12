
using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    public class UpdateCategorySpecValidator : AbstractValidator<UpdateCategorySpecDto>
    {
        public UpdateCategorySpecValidator()
        {
            RuleFor(cs => cs.DisplayOrder)
                .GreaterThanOrEqualTo(0);
        }
    }
}

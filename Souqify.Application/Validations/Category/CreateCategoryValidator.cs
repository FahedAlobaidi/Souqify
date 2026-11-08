using FluentValidation;
using Souqify.Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Validations.Category
{
    public class CreateCategoryValidator:AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Description).MaximumLength(2000);
            RuleFor(c=>c.DisplayOrder).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Slug).MaximumLength(100)
                .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
                .WithMessage("Slug can only use lowercase letters, numbers and single dashes, like graphic-cards")
                .When(c => c.Slug != null);
        }
    }
}

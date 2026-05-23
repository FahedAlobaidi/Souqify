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
        }
    }
}

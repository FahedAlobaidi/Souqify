

using FluentValidation;
using Souqify.Application.DTOs.Product;

namespace Souqify.Application.Validations
{
    public class CreateProductValidator: AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            RuleFor(p => p.Name).NotEmpty().MaximumLength(200);
            RuleFor(p => p.Description).NotEmpty().MaximumLength(2000);
            RuleFor(p => p.BasePrice).GreaterThan(0);
            RuleFor(p => p.CategoryId).NotEmpty();
            RuleFor(p => p.Brand).NotEmpty().MaximumLength(100);
            RuleFor(p => p.Variants).NotEmpty().WithMessage("At least one variant is required");
            RuleForEach(p => p.Variants).SetValidator(new CreateVariantValidator());
            RuleFor(p => p.ProductImages).NotEmpty().WithMessage("At least one image is required");
            RuleForEach(p => p.ProductImages).SetValidator(new CreateImageValidator());
            
        }
    }
}

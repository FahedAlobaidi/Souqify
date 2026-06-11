using FluentValidation;
using Souqify.Application.DTOs.Product;

namespace Souqify.Application.Validations.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(p => p.Name).NotEmpty().MaximumLength(200);
            RuleFor(p => p.Description).NotEmpty().MaximumLength(2000);
            RuleFor(p => p.BasePrice).GreaterThan(0);
            RuleFor(p => p.CategoryId).NotEmpty();
            RuleFor(p => p.Brand).NotEmpty().MaximumLength(100);
        }
    }
}

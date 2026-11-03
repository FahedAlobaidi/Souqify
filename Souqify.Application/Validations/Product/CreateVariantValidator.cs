using FluentValidation;
using Souqify.Application.DTOs.Variant;


namespace Souqify.Application.Validations.Product
{
    public class CreateVariantValidator : AbstractValidator<CreateVariantDto>
    {
        public CreateVariantValidator()
        {
            RuleFor(pv=>pv.VariantName).NotEmpty().MaximumLength(50).When(pv => pv.VariantName != null); ;
            RuleFor(pv => pv.SKU).NotEmpty().MaximumLength(50);
            RuleFor(pv => pv.StockQuantity).GreaterThanOrEqualTo(0);
            RuleFor(pv => pv.LowStockThreshold).GreaterThanOrEqualTo(0);
        }
    }
}

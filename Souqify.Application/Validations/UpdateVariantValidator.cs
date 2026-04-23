

using FluentValidation;
using Souqify.Application.DTOs.Variant;

namespace Souqify.Application.Validations
{
    public class UpdateVariantValidator:AbstractValidator<UpdateVariantDto>
    {
        public UpdateVariantValidator()
        {
            RuleFor(pv => pv.Size).MaximumLength(50).When(pv => pv.Size != null);
            RuleFor(pv => pv.Color).MaximumLength(50).When(pv => pv.Color != null);
            RuleFor(pv => pv.SKU).NotEmpty().MaximumLength(50);
            RuleFor(pv => pv.StockQuantity).GreaterThanOrEqualTo(0);
            RuleFor(pv => pv.LowStockThreshold).GreaterThanOrEqualTo(0);
        }
    }
}

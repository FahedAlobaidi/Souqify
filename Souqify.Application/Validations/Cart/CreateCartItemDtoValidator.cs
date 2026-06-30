using FluentValidation;
using Souqify.Application.DTOs.Cart;


namespace Souqify.Application.Validations.Cart
{
    public class CreateCartItemDtoValidator:AbstractValidator<CreateCartItemDto>
    {
        private const int MaxQuantityPerCartItem = 10; //// TODO: move to CartSettings/IOptions after B5

        public CreateCartItemDtoValidator()
        {
            RuleFor(ci => ci.VariantId).NotEmpty();
            RuleFor(ci => ci.ProductId).NotEmpty();
            RuleFor(ci => ci.Quantity).GreaterThan(0).LessThanOrEqualTo(MaxQuantityPerCartItem);
        }
    }
}

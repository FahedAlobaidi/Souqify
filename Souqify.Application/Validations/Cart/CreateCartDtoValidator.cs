using FluentValidation;
using Souqify.Application.DTOs.Cart;


namespace Souqify.Application.Validations.Cart
{
    public class CreateCartDtoValidator:AbstractValidator<CreateCartDto>
    {
        public CreateCartDtoValidator()
        {
            RuleFor(c => c.CartItem).NotNull().SetValidator(new CreateCartItemDtoValidator());
        }
    }
}

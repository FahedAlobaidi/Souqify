using FluentValidation;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Validations.Address;


namespace Souqify.Application.Validations.Order
{
    public class CreateOrderValidator: AbstractValidator<CreateOrderDto>
    {
        public CreateOrderValidator()
        {
            RuleFor(o => o.ShippingAddress)
                .NotNull()
                .SetValidator(new AddressDtoValidator());

            RuleFor(o => o.ContactPhone)
                .NotEmpty()
                .Length(10)
                .Matches(@"^\+?[0-9][0-9\s\-]*$")
                    .WithMessage("Contact phone may contain only digits, spaces and hyphens, with an optional leading +.");


            // Without this, a client can POST "paymentMethod": 47.
            // C# happily binds any int to an enum, and HasConversion<string>()
            // would then write "47" into the PaymentMethod column.
            RuleFor(o => o.PaymentMethod)
                .IsInEnum();
        }
    }
}

using FluentValidation;
using Souqify.Application.DTOs.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Validations.Address
{
    public class AddressDtoValidator: AbstractValidator<AddressDto>
    {
        public AddressDtoValidator()
        {
            RuleFor(a => a.Street)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(a => a.City)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(a => a.Region)
                .NotEmpty()
                .MaximumLength(100);

            //Optional fields: only check length when the client actually sent something
            RuleFor(a => a.PostalCode)
                .MaximumLength(20)
                .When(a => !string.IsNullOrWhiteSpace(a.PostalCode));

            RuleFor(a => a.DeliveryNote)
                .MaximumLength(300)
                .When(a => !string.IsNullOrWhiteSpace(a.DeliveryNote));
        }
    }
}

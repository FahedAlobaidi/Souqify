
using FluentValidation;
using Souqify.Application.DTOs.Admin;

namespace Souqify.Application.Validations.Specs
{
    public class CreateSpecAttributeValidator:AbstractValidator<CreateSpecAttributeDto>
    {
        public CreateSpecAttributeValidator()
        {
            RuleFor(sa => sa.Key)
                .Cascade(CascadeMode.Stop)          //stop at the first failure, so the later lines will never on a bad value
                .NotEmpty()
                .MaximumLength(50)
                .Matches("^[a-z0-9]+(_[a-z0-9]+)*$")
                    .WithMessage("Key may only use lower-case letters, digits and underscores.");

            RuleFor(sa => sa.Name)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(sa => sa.Unit)
                .MaximumLength(20);

            RuleFor(sa => sa.DataType)
                .IsInEnum();
        }
    }
}

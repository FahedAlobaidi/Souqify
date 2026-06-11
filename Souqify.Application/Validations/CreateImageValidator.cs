
using FluentValidation;
using Souqify.Application.DTOs.Image;

namespace Souqify.Application.Validations
{
    public class CreateImageValidator:AbstractValidator<CreateImageDto>
    {
        public CreateImageValidator()
        {
            RuleFor(pi => pi.ImageUrl).NotEmpty().WithMessage("Image Url required");
            RuleFor(pi => pi.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative");
        }
    }
}

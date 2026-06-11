

using FluentValidation;
using Souqify.Application.DTOs.Auth;

namespace Souqify.Application.Validations.Auth
{
    public class RegisterRequestValidator:AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(rr => rr.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(rr=>rr.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number is not valid.");
            RuleFor(rr => rr.Password).NotEmpty();
            RuleFor(rr => rr.ConfirmPassword).Equal(rr => rr.Password).WithMessage("Passwords do not match.");
            RuleFor(rr => rr.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(rr => rr.LastName).NotEmpty().MaximumLength(50);
        }
    }
}

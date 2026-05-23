using FluentValidation;
using Souqify.Application.DTOs.Auth;


namespace Souqify.Application.Validations.Auth
{
    public class LoginRequestValidator:AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(lr => lr.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(lr => lr.Password).NotEmpty();
        }
    }
}

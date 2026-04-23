using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Souqify.Filter
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach(var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator is null) continue;

                var result = await validator.ValidateAsync(new ValidationContext<object> (argument));

                if (!result.IsValid)
                {
                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(result.ToDictionary()));
                    return;
                }
            }

            await next();
        }
    }
}

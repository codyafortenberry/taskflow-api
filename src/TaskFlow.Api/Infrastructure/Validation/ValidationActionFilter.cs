using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskFlow.Api.Infrastructure.Validation;

/// <summary>
/// Runs any registered FluentValidation validator against each action argument
/// before the action executes. Failures throw a <see cref="ValidationException"/>,
/// which the global exception handler renders as a 400 ProblemDetails.
/// </summary>
public sealed class ValidationActionFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    throw new ValidationException(result.Errors);
                }
            }
        }

        await next();
    }
}

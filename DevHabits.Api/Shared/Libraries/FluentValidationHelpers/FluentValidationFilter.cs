using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevHabits.Api.Shared.Libraries.FluentValidationHelpers;

public sealed class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        foreach (object? arg in context.ActionArguments.Values) {
            if (arg == null)
                continue;

            Type validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());

            if (serviceProvider.GetService(validatorType) is not IValidator validator) {
                continue;
            }

            ValidationResult? result = await validator.ValidateAsync(new ValidationContext<object>(arg));
            if (result.IsValid)
                continue;

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(
                result.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key.ToLowerInvariant(),
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ));
            return;
        }

        await next();
    }
}

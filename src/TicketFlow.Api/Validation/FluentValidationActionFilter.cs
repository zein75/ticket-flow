using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TicketFlow.Api.Errors;

namespace TicketFlow.Api.Validation;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            foreach (var failure in result.Errors.Where(failure => failure is not null))
            {
                if (errors.TryGetValue(failure.PropertyName, out var existing))
                {
                    errors[failure.PropertyName] = existing.Append(failure.ErrorMessage).ToArray();
                    continue;
                }

                errors[failure.PropertyName] = [failure.ErrorMessage];
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(CreateValidationProblemDetails(context, errors));
            return;
        }

        await next();
    }

    public static ValidationProblemDetails CreateValidationProblemDetails(
        ActionContext context,
        IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Type = ApiErrorTypes.Validation,
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more request fields are invalid.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return problem;
    }
}

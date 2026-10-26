using FluentValidation;
using Lama.Application.Stockists;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lama.Api.Stockists;

/// <summary>Turns store errors into 400/404 responses the UI can show, instead of a 500.</summary>
public class StockistExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException validation:
                context.Result = new BadRequestObjectResult(new
                {
                    message = validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed",
                    errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
                context.ExceptionHandled = true;
                break;

            case StockistNotFoundException notFound:
                context.Result = new NotFoundObjectResult(new { message = notFound.Message });
                context.ExceptionHandled = true;
                break;

            case ArgumentException argument:
                // Strip the " (Parameter 'x')" suffix: the message is shown to the person as is
                var message = argument.ParamName == null
                    ? argument.Message
                    : argument.Message.Replace($" (Parameter '{argument.ParamName}')", string.Empty);
                context.Result = new BadRequestObjectResult(new { message });
                context.ExceptionHandled = true;
                break;
        }
    }
}

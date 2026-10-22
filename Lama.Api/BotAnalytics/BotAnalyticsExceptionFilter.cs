using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lama.Api.BotAnalytics;

/// <summary>A bad request is a 400 with the reasons, not a 500 the bot would keep retrying.</summary>
public class BotAnalyticsExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException validation:
                context.Result = new BadRequestObjectResult(new
                {
                    message = "Validation failed",
                    errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
                break;

            case ArgumentException argument:
                context.Result = new BadRequestObjectResult(new { message = argument.Message });
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }
}

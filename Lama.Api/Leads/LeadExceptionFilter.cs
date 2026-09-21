using FluentValidation;
using Lama.Application.LeadManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lama.Api.Leads;

/// <summary>
/// Turns lead errors into 400/404 responses instead of the default 500, so the bot
/// (which only retries 429/5xx) doesn't retry a payload that can never succeed.
/// </summary>
public class LeadExceptionFilter : IExceptionFilter
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
                context.ExceptionHandled = true;
                break;

            case LeadNotFoundException notFound:
                context.Result = new NotFoundObjectResult(new { message = notFound.Message });
                context.ExceptionHandled = true;
                break;

            case ArgumentException argument:
                context.Result = new BadRequestObjectResult(new { message = argument.Message });
                context.ExceptionHandled = true;
                break;
        }
    }
}

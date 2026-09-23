using FluentValidation;
using Lama.Application.AccessControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lama.Api.Auth;

/// <summary>Turns user-management errors into 400/404/409 instead of the default 500.</summary>
public class AuthExceptionFilter : IExceptionFilter
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

            case UserNotFoundException notFound:
                context.Result = new NotFoundObjectResult(new { message = notFound.Message });
                break;

            case EmailAlreadyUsedException duplicate:
                context.Result = new ConflictObjectResult(new { message = duplicate.Message, code = "email_taken" });
                break;

            case LastAdminException lastAdmin:
                context.Result = new ConflictObjectResult(new { message = lastAdmin.Message, code = "last_admin" });
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

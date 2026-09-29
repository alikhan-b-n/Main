using FluentValidation;
using Lama.Application.Scheduling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lama.Api.Scheduling;

/// <summary>
/// Booking errors the caller can act on. A slot taken in the meantime is a 409, so the
/// bot can reload the day instead of showing a failure; an unreachable calendar is a 503,
/// which tells the bot to fall back to "a manager will contact you".
/// </summary>
public class SchedulingExceptionFilter : IExceptionFilter
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

            case AppointmentNotFoundException notFound:
                context.Result = new NotFoundObjectResult(new { message = notFound.Message });
                break;

            case SlotUnavailableException taken:
                context.Result = new ConflictObjectResult(new { message = taken.Message, code = "slot_taken" });
                break;

            case BookingDisabledException disabled:
                context.Result = new ObjectResult(new { message = disabled.Message, code = "booking_disabled" })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                break;

            case CalendarUnavailableException unavailable:
                context.Result = new ObjectResult(new { message = unavailable.Message, code = "calendar_unavailable" })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                break;

            case ArgumentException argument:
                context.Result = new BadRequestObjectResult(new { message = argument.Message });
                break;

            case InvalidOperationException invalid:
                context.Result = new ConflictObjectResult(new { message = invalid.Message });
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }
}

using Lama.Api.Integrations.TelegramBot;
using Lama.Api.Scheduling;
using Lama.Application.Scheduling;
using Lama.Application.Scheduling.Commands;
using Lama.Application.Scheduling.Queries;
using Lama.Domain.Scheduling.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>
/// What the bot needs to show a calendar: which days are open, what a day looks like,
/// and the booking itself. Same API key as the lead intake; no user session involved.
/// </summary>
[ApiController]
[Route("api/integrations/telegram/scheduling")]
[AllowAnonymous]
[TypeFilter(typeof(TelegramBotApiKeyFilter))]
[TypeFilter(typeof(SchedulingExceptionFilter))]
public class TelegramSchedulingController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelegramSchedulingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Open days with the number of free slots in each.</summary>
    [HttpGet("days")]
    public async Task<ActionResult<AvailabilityDto>> GetDays(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAvailabilityQuery(), cancellationToken));

    /// <summary>Every slot of one day: the bot greys out the taken ones.</summary>
    [HttpGet("days/{date}")]
    public async Task<ActionResult<DaySlotsDto>> GetDay(DateOnly date, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDaySlotsQuery(date), cancellationToken));

    /// <summary>
    /// 201 with the Meet link, 409 when the slot was taken while the person was choosing,
    /// 503 when booking is off or Google is unreachable.
    /// </summary>
    [HttpPost("book")]
    public async Task<ActionResult<BookAppointmentResult>> Book(
        [FromBody] TelegramBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new BookAppointmentCommand(
            request.LeadId,
            request.FullName,
            request.Email,
            request.StartUtc,
            request.Phone,
            request.TelegramUsername,
            request.TelegramId,
            AppointmentKind.IntroCall,
            request.Comment), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Человек отменяет свою запись из бота: например, чтобы записаться заново
    /// с исправленной почтой. Чужую запись отменить нельзя — сверяем Telegram id.
    /// </summary>
    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] TelegramCancelRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelOwnAppointmentCommand(id, request.TelegramId, request.Reason), cancellationToken);
        return NoContent();
    }
}

public record TelegramCancelRequest(long TelegramId, string? Reason);

/// <summary>
/// <see cref="StartUtc"/> is a slot start exactly as the API handed it out.
/// <see cref="LeadId"/> is the CRM id from the intake response, when the bot has it.
/// </summary>
public record TelegramBookingRequest(
    Guid? LeadId,
    string FullName,
    string Email,
    DateTime StartUtc,
    string? Phone,
    string? TelegramUsername,
    long? TelegramId,
    string? Comment);

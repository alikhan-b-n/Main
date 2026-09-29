using Lama.Api.Scheduling;
using Lama.Application.LeadManagement;
using Lama.Application.Scheduling;
using Lama.Application.Scheduling.Commands;
using Lama.Application.Scheduling.Queries;
using Lama.Domain.AccessControl.Entities;
using Lama.Domain.Scheduling.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>Booked calls and the schedule behind them, as managed from the CRM.</summary>
[ApiController]
[Route("api/scheduling")]
[TypeFilter(typeof(SchedulingExceptionFilter))]
public class SchedulingController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchedulingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // --- Schedule -----------------------------------------------------------

    [HttpGet("settings")]
    public async Task<ActionResult<AvailabilitySettingsDto>> GetSettings(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAvailabilitySettingsQuery(), cancellationToken));

    /// <summary>Working days and hours are an administrator's call.</summary>
    [HttpPut("settings")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<AvailabilitySettingsDto>> UpdateSettings(
        [FromBody] UpdateAvailabilitySettingsRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new UpdateAvailabilitySettingsCommand(
            request.Enabled,
            request.TimeZoneId,
            request.SlotMinutes,
            request.MinLeadTimeHours,
            request.MaxDaysAhead,
            request.BufferMinutes,
            request.Windows ?? Array.Empty<WorkingWindowDto>()), cancellationToken));

    // --- Free time ----------------------------------------------------------

    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityDto>> GetAvailability(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAvailabilityQuery(), cancellationToken));

    [HttpGet("availability/{date}")]
    public async Task<ActionResult<DaySlotsDto>> GetDay(DateOnly date, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDaySlotsQuery(date), cancellationToken));

    // --- Appointments -------------------------------------------------------

    [HttpGet("appointments")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetAppointments(
        [FromQuery] string? status,
        [FromQuery] string? kind,
        [FromQuery] Guid? leadId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] bool upcomingFirst = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseOptional<AppointmentStatus>(status, out var parsedStatus))
            return BadRequest(Invalid(nameof(status), status, Enum.GetNames<AppointmentStatus>()));
        if (!TryParseOptional<AppointmentKind>(kind, out var parsedKind))
            return BadRequest(Invalid(nameof(kind), kind, Enum.GetNames<AppointmentKind>()));

        var filter = new AppointmentFilter(
            parsedStatus, parsedKind, leadId,
            from?.ToUniversalTime(), to?.ToUniversalTime(),
            search, upcomingFirst, page, pageSize);

        return Ok(await _mediator.Send(new GetAppointmentsQuery(filter), cancellationToken));
    }

    [HttpGet("appointments/{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _mediator.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return appointment == null ? NotFound(new { message = $"Appointment with ID {id} not found" }) : Ok(appointment);
    }

    /// <summary>A manager books a time on someone's behalf, e.g. after a phone call.</summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<BookAppointmentResult>> Book(
        [FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseOptional<AppointmentKind>(request.Kind, out var kind))
            return BadRequest(Invalid(nameof(request.Kind), request.Kind, Enum.GetNames<AppointmentKind>()));

        var result = await _mediator.Send(new BookAppointmentCommand(
            request.LeadId,
            request.FullName,
            request.Email,
            request.StartUtc,
            request.Phone,
            request.TelegramUsername,
            request.TelegramId,
            kind ?? AppointmentKind.IntroCall,
            request.Comment), cancellationToken);

        return CreatedAtAction(nameof(GetAppointment), new { id = result.AppointmentId }, result);
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelAppointmentRequest? request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelAppointmentCommand(id, request?.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("appointments/{id:guid}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new RescheduleAppointmentCommand(id, request.StartUtc), cancellationToken));

    [HttpPost("appointments/{id:guid}/close")]
    public async Task<IActionResult> Close(
        Guid id, [FromBody] CloseAppointmentRequest? request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CloseAppointmentCommand(id, request?.NoShow ?? false), cancellationToken);
        return NoContent();
    }

    private static bool TryParseOptional<T>(string? value, out T? parsed) where T : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!Enum.TryParse<T>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
            return false;

        parsed = result;
        return true;
    }

    private static object Invalid(string name, string? value, string[] allowed) =>
        new { message = $"Invalid {name}: {value}. Valid values: {string.Join(", ", allowed)}" };
}

public record UpdateAvailabilitySettingsRequest(
    bool Enabled,
    string TimeZoneId,
    int SlotMinutes,
    int MinLeadTimeHours,
    int MaxDaysAhead,
    int BufferMinutes,
    IReadOnlyList<WorkingWindowDto>? Windows);

public record BookAppointmentRequest(
    Guid? LeadId,
    string FullName,
    string Email,
    DateTime StartUtc,
    string? Phone,
    string? TelegramUsername,
    long? TelegramId,
    string? Kind,
    string? Comment);

public record CancelAppointmentRequest(string? Reason);

public record RescheduleAppointmentRequest(DateTime StartUtc);

public record CloseAppointmentRequest(bool NoShow);

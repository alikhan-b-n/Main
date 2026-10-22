using Lama.Api.BotAnalytics;
using Lama.Application.BotAnalytics;
using Lama.Domain.AccessControl.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>How people go through the bot's survey, for the "bot statistics" page.</summary>
[ApiController]
[Route("api/bot-stats")]
[TypeFilter(typeof(BotAnalyticsExceptionFilter))]
public class BotStatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BotStatsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <param name="days">Length of the period, ending now.</param>
    /// <param name="source">Only sessions from this source; "unknown" for those without one.</param>
    /// <param name="timeZone">The viewer's IANA zone, for the by-hour chart.</param>
    [HttpGet]
    public async Task<ActionResult<BotStatsDto>> Get(
        [FromQuery] int days = 30,
        [FromQuery] string? source = null,
        [FromQuery] string? timeZone = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetBotStatsQuery(days, source, timeZone), cancellationToken));

    [HttpGet("reminder-settings")]
    public async Task<ActionResult<BotReminderSettingsDto>> GetReminderSettings(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetBotReminderSettingsQuery(), cancellationToken));

    /// <summary>Whether the bot writes to people who stopped is an administrator's call.</summary>
    [HttpPut("reminder-settings")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<BotReminderSettingsDto>> UpdateReminderSettings(
        [FromBody] UpdateBotReminderSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!TimeOnly.TryParse(request.QuietFrom, out var quietFrom) || !TimeOnly.TryParse(request.QuietTo, out var quietTo))
            return BadRequest(new { message = "Quiet hours must look like 22:00" });

        return Ok(await _mediator.Send(new UpdateBotReminderSettingsCommand(
            request.Enabled, request.DelayHours, quietFrom, quietTo, request.TimeZoneId), cancellationToken));
    }
}

public record UpdateBotReminderSettingsRequest(
    bool Enabled, int DelayHours, string QuietFrom, string QuietTo, string TimeZoneId);

using Lama.Api.BotAnalytics;
using Lama.Api.Integrations.TelegramBot;
using Lama.Application.BotAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>
/// What the bot reports about the survey, and the reminder settings it follows.
/// Same API key as the lead intake; no user session involved.
/// </summary>
[ApiController]
[Route("api/integrations/telegram")]
[AllowAnonymous]
[TypeFilter(typeof(TelegramBotApiKeyFilter))]
[TypeFilter(typeof(BotAnalyticsExceptionFilter))]
public class TelegramSurveyEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelegramSurveyEventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>A batch of survey events. Sending the same batch again stores nothing new.</summary>
    [HttpPost("survey-events")]
    public async Task<ActionResult<RecordSurveyEventsResult>> Record(
        [FromBody] SurveyEventsRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new RecordSurveyEventsCommand(request.Events ?? Array.Empty<SurveyEventInput>()), cancellationToken));

    [HttpGet("reminder-settings")]
    public async Task<ActionResult<BotReminderSettingsDto>> GetReminderSettings(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetBotReminderSettingsQuery(), cancellationToken));
}

public record SurveyEventsRequest(IReadOnlyList<SurveyEventInput>? Events);

using System.Text.Json;
using Lama.Api.Integrations.TelegramBot;
using Lama.Api.Leads;
using Lama.Application.LeadManagement.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>Intake for completed surveys from the IconicU Telegram bot.</summary>
[ApiController]
[Route("api/integrations/telegram/leads")]
[TypeFilter(typeof(TelegramBotApiKeyFilter))]
[TypeFilter(typeof(LeadExceptionFilter))]
public class TelegramLeadsController : ControllerBase
{
    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    private readonly IMediator _mediator;
    private readonly TimeProvider _clock;

    public TelegramLeadsController(IMediator mediator, TimeProvider clock)
    {
        _mediator = mediator;
        _clock = clock;
    }

    /// <summary>
    /// 201 for a new lead, 200 when an open lead of the same person was updated
    /// or the bot retried a submission that is already stored.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SubmitLeadResponse>> Submit([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return BadRequest(new { message = "Payload must be a JSON object" });

        string raw;
        TelegramLeadPayload? payload;
        try
        {
            raw = body.GetRawText();
            payload = JsonSerializer.Deserialize<TelegramLeadPayload>(raw, PayloadJson);
        }
        // InvalidOperationException: the body parsed as JSON but its strings are not valid UTF-8
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return BadRequest(new { message = $"Malformed payload: {ex.Message}" });
        }

        if (payload == null)
            return BadRequest(new { message = "Payload must be a JSON object" });

        var submission = payload.ToSubmission(raw, _clock.GetUtcNow().UtcDateTime);
        var result = await _mediator.Send(new SubmitTelegramLeadCommand(submission), cancellationToken);
        var response = new SubmitLeadResponse(result.LeadId, result.Outcome.ToString());

        return result.Outcome == SubmitLeadOutcome.Created
            ? CreatedAtAction(nameof(LeadsController.GetLead), "Leads", new { id = result.LeadId }, response)
            : Ok(response);
    }
}

public record SubmitLeadResponse(Guid LeadId, string Outcome);

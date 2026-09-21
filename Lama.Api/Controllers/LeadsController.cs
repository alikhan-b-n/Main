using Lama.Api.Leads;
using Lama.Application.LeadManagement;
using Lama.Application.LeadManagement.Commands;
using Lama.Application.LeadManagement.Queries;
using Lama.Domain.LeadManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/leads")]
[TypeFilter(typeof(LeadExceptionFilter))]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadListItemDto>>> GetLeads(
        [FromQuery] string? status,
        [FromQuery] string? temperature,
        [FromQuery] string? source,
        [FromQuery] string? degree,
        [FromQuery] string? country,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseOptional<LeadStatus>(status, out var parsedStatus))
            return BadRequest(new { message = $"Invalid status: {status}. Valid values: {string.Join(", ", Enum.GetNames<LeadStatus>())}" });
        if (!TryParseOptional<LeadTemperature>(temperature, out var parsedTemperature))
            return BadRequest(new { message = $"Invalid temperature: {temperature}. Valid values: {string.Join(", ", Enum.GetNames<LeadTemperature>())}" });

        var filter = new LeadFilter(
            parsedStatus,
            parsedTemperature,
            source,
            degree,
            country,
            search,
            from?.ToUniversalTime(),
            to?.ToUniversalTime(),
            page,
            pageSize);

        return Ok(await _mediator.Send(new GetLeadsQuery(filter), cancellationToken));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<LeadStatsDto>> GetStats(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetLeadStatsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDetailsDto>> GetLead(Guid id, CancellationToken cancellationToken)
    {
        var lead = await _mediator.Send(new GetLeadByIdQuery(id), cancellationToken);
        return lead == null ? NotFound() : Ok(lead);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeLeadStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LeadStatus>(request.Status, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            return BadRequest(new { message = $"Invalid status: {request.Status}. Valid values: {string.Join(", ", Enum.GetNames<LeadStatus>())}" });

        await _mediator.Send(new ChangeLeadStatusCommand(id, status, request.LostReason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddLeadNoteRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddLeadNoteCommand(id, request.Text), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLead(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLeadCommand(id), cancellationToken);
        return NoContent();
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? result) where TEnum : struct, Enum
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return false;

        result = parsed;
        return true;
    }
}

public record ChangeLeadStatusRequest(string Status, string? LostReason);

public record AddLeadNoteRequest(string Text);

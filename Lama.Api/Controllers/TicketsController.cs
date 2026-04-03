using Lama.Application.Common;
using Lama.Application.TicketManagement.Commands;
using Lama.Application.TicketManagement.Queries;
using Lama.Domain.CustomerService.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/support-cases")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Ticket> _ticketRepository;

    public TicketsController(IMediator mediator, IRepository<Ticket> ticketRepository)
    {
        _mediator = mediator;
        _ticketRepository = ticketRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateTicket([FromBody] CreateSupportCaseRequest request)
    {
        if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var priority))
            return BadRequest(new { message = $"Invalid priority: {request.Priority}" });

        if (!Enum.TryParse<TicketSource>(request.Source, ignoreCase: true, out var source))
            return BadRequest(new { message = $"Invalid source: {request.Source}" });

        var command = new CreateTicketCommand(
            request.TicketName,
            request.Description,
            priority,
            source,
            request.ContactId,
            request.CompanyId
        );

        var ticketId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTicket), new { id = ticketId }, ticketId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ticket>> GetTicket(Guid id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);

        if (ticket == null)
            return NotFound();

        return Ok(ticket);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupportCaseDto>>> GetAllTickets()
    {
        var query = new GetAllTicketsQuery();
        var tickets = await _mediator.Send(query);

        return Ok(tickets);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateTicketCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch" });

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCaseStatusRequest request)
    {
        if (!Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new { message = $"Invalid status: {request.Status}" });

        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null) return NotFound();

        ticket.UpdateStatus(status);
        await _ticketRepository.UpdateAsync(ticket);
        return NoContent();
    }

    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromBody] UpdateCasePriorityRequest request)
    {
        if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var priority))
            return BadRequest(new { message = $"Invalid priority: {request.Priority}" });

        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null) return NotFound();

        ticket.UpdatePriority(priority);
        await _ticketRepository.UpdateAsync(ticket);
        return NoContent();
    }

    [HttpPost("{id:guid}/summarize")]
    public async Task<IActionResult> SummarizeTicket(Guid id)
    {
        try
        {
            var summary = await _mediator.Send(new SummarizeSupportCaseQuery(id));
            return Ok(new { summary });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var command = new DeleteTicketCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

public record CreateSupportCaseRequest(
    string TicketName,
    string Description,
    string Priority,
    string Source,
    Guid? ContactId = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null
);

public record UpdateCaseStatusRequest(string Status);
public record UpdateCasePriorityRequest(string Priority);

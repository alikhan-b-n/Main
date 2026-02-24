using Lama.Application.Common;
using Lama.Application.TicketManagement.Commands;
using Lama.Application.TicketManagement.Queries;
using Lama.Domain.CustomerService.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/objects/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Ticket> _ticketRepository;

    public TicketsController(
        IMediator mediator,
        IRepository<Ticket> ticketRepository)
    {
        _mediator = mediator;
        _ticketRepository = ticketRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateTicket([FromBody] CreateTicketCommand command)
    {
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
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var command = new DeleteTicketCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

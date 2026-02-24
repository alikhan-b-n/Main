using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Application.SalesManagement.Queries;
using Lama.Domain.SalesManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/objects/deals")]
public class DealsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Deal> _dealRepository;

    public DealsController(
        IMediator mediator,
        IRepository<Deal> dealRepository)
    {
        _mediator = mediator;
        _dealRepository = dealRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateDeal([FromBody] CreateDealCommand command)
    {
        var dealId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDeal), new { id = dealId }, dealId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Deal>> GetDeal(Guid id)
    {
        var deal = await _dealRepository.GetByIdAsync(id);

        if (deal == null)
            return NotFound();

        return Ok(deal);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DealDto>>> GetAllDeals()
    {
        var query = new GetAllDealsQuery();
        var deals = await _mediator.Send(query);

        return Ok(deals);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDeal(Guid id, [FromBody] UpdateDealCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch" });

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDeal(Guid id)
    {
        var command = new DeleteDealCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

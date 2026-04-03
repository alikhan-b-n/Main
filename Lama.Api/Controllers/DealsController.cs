using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Application.SalesManagement.Queries;
using Lama.Domain.SalesManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/opportunities")]
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
    public async Task<ActionResult<OpportunityDto>> GetDeal(Guid id)
    {
        var deal = await _dealRepository.GetByIdAsync(id);

        if (deal == null)
            return NotFound();

        return Ok(new OpportunityDto(
            deal.Id,
            deal.Name,
            deal.CompanyId,
            deal.Probability,
            deal.Amount.Amount,
            deal.ExpectedCloseDate,
            deal.Amount.Currency,
            deal.Status.ToString(),
            deal.CreatedAt
        ));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetAllDeals()
    {
        var query = new GetAllDealsQuery();
        var deals = await _mediator.Send(query);

        var result = deals.Select(d => new OpportunityDto(
            d.Id,
            d.Name,
            d.CompanyId,
            d.Probability,
            d.Amount,
            d.ExpectedCloseDate,
            d.Currency,
            d.Status,
            d.CreatedAt
        ));

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDeal(Guid id, [FromBody] UpdateDealCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch" });

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOpportunityStatusRequest request)
    {
        if (!Enum.TryParse<OpportunityStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new { message = $"Invalid status: {request.Status}. Valid values: Relevant, RealizedRevenue, NotRelevant" });

        var deal = await _dealRepository.GetByIdAsync(id);
        if (deal == null) return NotFound();

        deal.UpdateStatus(status);
        await _dealRepository.UpdateAsync(deal);

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

public record UpdateOpportunityStatusRequest(string Status);

public record OpportunityDto(
    Guid Id,
    string Name,
    Guid AccountId,
    int Probability,
    decimal ExpectedRevenue,
    DateTime ExpectedCloseDate,
    string Currency,
    string Status,
    DateTime CreatedAt
);

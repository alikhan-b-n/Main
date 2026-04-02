using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Application.SalesManagement.Queries;
using Lama.Domain.SalesManagement.Entities;
using Lama.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/opportunities")]
public class DealsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Deal> _dealRepository;
    private readonly ApplicationDbContext _dbContext;

    public DealsController(
        IMediator mediator,
        IRepository<Deal> dealRepository,
        ApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _dealRepository = dealRepository;
        _dbContext = dbContext;
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

        var stage = await _dbContext.Stages.FindAsync(deal.StageId);

        return Ok(new OpportunityDto(
            deal.Id,
            deal.Name,
            deal.CompanyId,
            stage?.Name ?? "Unknown",
            deal.Probability,
            deal.Amount.Amount,
            deal.ExpectedCloseDate,
            deal.Amount.Currency,
            deal.CreatedAt
        ));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetAllDeals()
    {
        var query = new GetAllDealsQuery();
        var deals = await _mediator.Send(query);

        var stages = await _dbContext.Stages
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var result = deals.Select(d => new OpportunityDto(
            d.Id,
            d.Name,
            d.CompanyId,
            stages.TryGetValue(d.StageId, out var stageName) ? stageName : "Unknown",
            d.Probability,
            d.Amount,
            d.ExpectedCloseDate,
            d.Currency,
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDeal(Guid id)
    {
        var command = new DeleteDealCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

public record OpportunityDto(
    Guid Id,
    string Name,
    Guid AccountId,
    string Stage,
    int Probability,
    decimal ExpectedRevenue,
    DateTime ExpectedCloseDate,
    string Currency,
    DateTime CreatedAt
);

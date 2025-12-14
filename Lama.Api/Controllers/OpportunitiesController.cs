using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Domain.SalesManagement.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly ICommandHandler<CreateOpportunityCommand, Guid> _createOpportunityHandler;
    private readonly IRepository<Opportunity> _opportunityRepository;

    public OpportunitiesController(
        ICommandHandler<CreateOpportunityCommand, Guid> createOpportunityHandler,
        IRepository<Opportunity> opportunityRepository)
    {
        _createOpportunityHandler = createOpportunityHandler;
        _opportunityRepository = opportunityRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateOpportunity([FromBody] CreateOpportunityCommand command)
    {
        var opportunityId = await _createOpportunityHandler.Handle(command);
        return CreatedAtAction(nameof(GetOpportunity), new { id = opportunityId }, opportunityId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Opportunity>> GetOpportunity(Guid id)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(id);

        if (opportunity == null)
            return NotFound();

        return Ok(opportunity);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Opportunity>>> GetAllOpportunities()
    {
        var opportunities = await _opportunityRepository.GetAllAsync();
        return Ok(opportunities);
    }
}

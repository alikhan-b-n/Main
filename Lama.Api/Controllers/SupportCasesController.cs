using Lama.Application.Common;
using Lama.Application.CustomerService.Commands;
using Lama.Domain.CustomerService.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportCasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<SupportCase> _supportCaseRepository;

    public SupportCasesController(
        IMediator mediator,
        IRepository<SupportCase> supportCaseRepository)
    {
        _mediator = mediator;
        _supportCaseRepository = supportCaseRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateSupportCase([FromBody] CreateSupportCaseCommand command)
    {
        var caseId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSupportCase), new { id = caseId }, caseId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupportCase>> GetSupportCase(Guid id)
    {
        var supportCase = await _supportCaseRepository.GetByIdAsync(id);

        if (supportCase == null)
            return NotFound();

        return Ok(supportCase);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupportCase>>> GetAllSupportCases()
    {
        var supportCases = await _supportCaseRepository.GetAllAsync();
        return Ok(supportCases);
    }
}

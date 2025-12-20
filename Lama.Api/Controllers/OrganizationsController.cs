using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Organization> _organizationRepository;

    public OrganizationsController(
        IMediator mediator,
        IRepository<Organization> organizationRepository)
    {
        _mediator = mediator;
        _organizationRepository = organizationRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateOrganization([FromBody] CreateOrganizationCommand command)
    {
        var organizationId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrganization), new { id = organizationId }, organizationId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(Guid id)
    {
        var organization = await _organizationRepository.GetByIdAsync(id);

        if (organization == null)
            return NotFound();

        return Ok(organization);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> GetAllOrganizations()
    {
        var organizations = await _organizationRepository.GetAllAsync();
        return Ok(organizations);
    }
}

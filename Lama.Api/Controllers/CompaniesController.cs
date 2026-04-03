using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Company> _companyRepository;

    public CompaniesController(
        IMediator mediator,
        IRepository<Company> companyRepository)
    {
        _mediator = mediator;
        _companyRepository = companyRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        var companyId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCompany), new { id = companyId }, companyId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetCompany(Guid id)
    {
        var query = new GetCompanyByIdQuery(id);
        var company = await _mediator.Send(query);

        if (company == null)
            return NotFound();

        return Ok(new AccountDto(company.Id, company.Name, company.Industry, company.Website, company.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAllCompanies()
    {
        var query = new GetAllCompaniesQuery();
        var companies = await _mediator.Send(query);
        var result = companies.Select(c => new AccountDto(c.Id, c.Name, c.Industry, c.Website, c.CreatedAt));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch" });

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        var command = new DeleteCompanyCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

public record AccountDto(Guid Id, string Name, string? Industry, string? Website, DateTime CreatedAt);

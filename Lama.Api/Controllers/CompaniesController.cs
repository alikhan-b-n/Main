using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Domain.CustomerManagement.Entities;
using Lama.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/objects/companies")]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Company> _companyRepository;
    private readonly ApplicationDbContext _dbContext;

    public CompaniesController(
        IMediator mediator,
        IRepository<Company> companyRepository,
        ApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _companyRepository = companyRepository;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        var companyId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCompany), new { id = companyId }, companyId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDto>> GetCompany(Guid id)
    {
        var query = new GetCompanyByIdQuery(id);
        var company = await _mediator.Send(query);

        if (company == null)
            return NotFound();

        return Ok(company);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAllCompanies()
    {
        var query = new GetAllCompaniesQuery();
        var companies = await _mediator.Send(query);

        return Ok(companies);
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

    [HttpPut("{companyId}/associations/client_categories/{categoryId}")]
    public async Task<IActionResult> AssociateClientCategory(Guid companyId, Guid categoryId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null) return NotFound(new { message = "Company not found" });

        var category = await _dbContext.ClientCategories.FindAsync(categoryId);
        if (category == null) return NotFound(new { message = "Client category not found" });

        company.AssignToCategory(categoryId);
        await _companyRepository.UpdateAsync(company);

        return NoContent();
    }
}

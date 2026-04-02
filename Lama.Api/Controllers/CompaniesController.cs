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
[Route("api/accounts")]
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
    public async Task<ActionResult<AccountDto>> GetCompany(Guid id)
    {
        var query = new GetCompanyByIdQuery(id);
        var company = await _mediator.Send(query);

        if (company == null)
            return NotFound();

        var categoryName = company.ClientCategoryId.HasValue
            ? (await _dbContext.ClientCategories.FindAsync(company.ClientCategoryId.Value))?.Name ?? "Prospect"
            : "Prospect";

        return Ok(new AccountDto(company.Id, company.Name, company.Industry, company.Website, categoryName, company.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAllCompanies()
    {
        var query = new GetAllCompaniesQuery();
        var companies = await _mediator.Send(query);

        var categories = await _dbContext.ClientCategories
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var result = companies.Select(c => new AccountDto(
            c.Id,
            c.Name,
            c.Industry,
            c.Website,
            c.ClientCategoryId.HasValue && categories.TryGetValue(c.ClientCategoryId.Value, out var name) ? name : "Prospect",
            c.CreatedAt
        ));

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

public record AccountDto(
    Guid Id,
    string Name,
    string? Industry,
    string? Website,
    string Type,
    DateTime CreatedAt
);

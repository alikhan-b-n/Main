using Lama.Api.AiRequests;
using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Domain.CustomerManagement.Entities;
using Lama.Domain.CustomerService.Entities;
using Lama.Domain.SalesManagement.Entities;
using Lama.Infrastructure.Persistence;
using Lama.Integrations.AI.Exceptions;
using Lama.Integrations.AI.Interfaces;
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
    private readonly ITextAiService _aiService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        IMediator mediator,
        IRepository<Company> companyRepository,
        ApplicationDbContext dbContext,
        ITextAiService aiService,
        ILogger<CompaniesController> logger)
    {
        _mediator = mediator;
        _companyRepository = companyRepository;
        _dbContext = dbContext;
        _aiService = aiService;
        _logger = logger;
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

    [HttpGet("{id:guid}/health")]
    public async Task<IActionResult> GetAccountHealth(Guid id, [FromQuery] AiRequestFields aiRequest)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null) return NotFound();

        var now = DateTime.UtcNow;

        var totalContacts = await _dbContext.Contacts.CountAsync(c => c.CompanyId == id);

        var openTickets = await _dbContext.Tickets
            .Where(t => t.CompanyId == id &&
                        (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting))
            .ToListAsync();

        var openCases = openTickets.Count;
        var criticalCases = openTickets.Count(t => t.Priority == TicketPriority.Urgent);

        var relevantOpportunities = await _dbContext.Deals
            .CountAsync(d => d.CompanyId == id && d.Status == OpportunityStatus.Relevant);

        var lastContact = await _dbContext.Contacts
            .Where(c => c.CompanyId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => (DateTime?)c.CreatedAt)
            .FirstOrDefaultAsync();

        int? daysSinceLastContact = lastContact.HasValue
            ? (int)(now - lastContact.Value).TotalDays
            : null;

        var ctx = new AccountHealthContext(
            company.Name,
            company.Industry,
            totalContacts,
            openCases,
            criticalCases,
            relevantOpportunities,
            daysSinceLastContact
        );

        try
        {
            var summary = await _aiService.GenerateAccountHealthAsync(ctx, aiRequest.ToProviderOptions());
            return Ok(new { summary, data = ctx });
        }
        catch (Exception ex) when (AiRequestExceptionHandling.TryHandle(ex, _logger) is { } result)
        {
            return result;
        }
    }
}

public record AccountDto(Guid Id, string Name, string? Industry, string? Website, DateTime CreatedAt);

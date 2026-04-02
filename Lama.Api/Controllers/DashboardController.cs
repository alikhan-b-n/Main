using Lama.Domain.CustomerService.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var totalAccounts = await _dbContext.Companies.CountAsync();
        var totalContacts = await _dbContext.Contacts.CountAsync();
        var totalOpportunities = await _dbContext.Deals.CountAsync();

        var openCases = await _dbContext.Tickets.CountAsync(t =>
            t.Status == TicketStatus.Open ||
            t.Status == TicketStatus.InProgress ||
            t.Status == TicketStatus.Waiting);

        var closedStageIds = await _dbContext.Stages
            .Where(s => s.IsClosed)
            .Select(s => s.Id)
            .ToListAsync();

        var deals = await _dbContext.Deals.ToListAsync();

        var pipelineValue = deals
            .Where(d => !closedStageIds.Contains(d.StageId))
            .Sum(d => d.Amount.Amount);

        var now = DateTime.UtcNow;
        var wonDealsThisMonth = deals.Count(d =>
            d.ActualCloseDate.HasValue &&
            d.ActualCloseDate.Value.Year == now.Year &&
            d.ActualCloseDate.Value.Month == now.Month &&
            closedStageIds.Contains(d.StageId));

        var conversionRate = totalOpportunities > 0
            ? (int)Math.Round((double)wonDealsThisMonth / totalOpportunities * 100)
            : 0;

        return Ok(new DashboardStatsDto(
            totalAccounts,
            totalContacts,
            totalOpportunities,
            openCases,
            pipelineValue,
            wonDealsThisMonth,
            conversionRate
        ));
    }
}

public record DashboardStatsDto(
    int TotalAccounts,
    int TotalContacts,
    int TotalOpportunities,
    int OpenCases,
    decimal PipelineValue,
    int WonDealsThisMonth,
    int ConversionRate
);

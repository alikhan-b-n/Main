using Lama.Domain.CustomerService.Entities;
using Lama.Domain.SalesManagement.Entities;
using Lama.Infrastructure.Persistence;
using Lama.Integrations.AI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITextAiService _aiService;

    public DashboardController(ApplicationDbContext dbContext, ITextAiService aiService)
    {
        _dbContext = dbContext;
        _aiService = aiService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var now = DateTime.UtcNow;
        var firstDayOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstDayOfPreviousMonth = firstDayOfCurrentMonth.AddMonths(-1);

        var totalAccounts = await _dbContext.Companies.CountAsync();
        var totalContacts = await _dbContext.Contacts.CountAsync();
        var totalOpportunities = await _dbContext.Deals.CountAsync();

        var openCases = await _dbContext.Tickets.CountAsync(t =>
            t.Status == TicketStatus.Open ||
            t.Status == TicketStatus.InProgress ||
            t.Status == TicketStatus.Waiting);

        var deals = await _dbContext.Deals.ToListAsync();

        var pipelineValue = deals
            .Where(d => d.Status == OpportunityStatus.Relevant)
            .Sum(d => d.Amount.Amount);

        var wonDealsThisMonth = deals.Count(d =>
            d.ActualCloseDate.HasValue &&
            d.ActualCloseDate.Value.Year == now.Year &&
            d.ActualCloseDate.Value.Month == now.Month &&
            d.Status == OpportunityStatus.RealizedRevenue);

        var realizedTotal = deals.Count(d => d.Status == OpportunityStatus.RealizedRevenue);
        var notRelevantTotal = deals.Count(d => d.Status == OpportunityStatus.NotRelevant);
        var closedTotal = realizedTotal + notRelevantTotal;
        var conversionRate = closedTotal > 0
            ? (int)Math.Round((double)realizedTotal / closedTotal * 100)
            : 0;

        // Trend calculations: current month vs previous month
        var currentAccounts = await _dbContext.Companies
            .CountAsync(c => c.CreatedAt >= firstDayOfCurrentMonth);
        var previousAccounts = await _dbContext.Companies
            .CountAsync(c => c.CreatedAt >= firstDayOfPreviousMonth && c.CreatedAt < firstDayOfCurrentMonth);

        var currentContacts = await _dbContext.Contacts
            .CountAsync(c => c.CreatedAt >= firstDayOfCurrentMonth);
        var previousContacts = await _dbContext.Contacts
            .CountAsync(c => c.CreatedAt >= firstDayOfPreviousMonth && c.CreatedAt < firstDayOfCurrentMonth);

        var currentOpportunities = await _dbContext.Deals
            .CountAsync(d => d.CreatedAt >= firstDayOfCurrentMonth);
        var previousOpportunities = await _dbContext.Deals
            .CountAsync(d => d.CreatedAt >= firstDayOfPreviousMonth && d.CreatedAt < firstDayOfCurrentMonth);

        var currentCases = await _dbContext.Tickets
            .CountAsync(t => t.CreatedAt >= firstDayOfCurrentMonth &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting));
        var previousCases = await _dbContext.Tickets
            .CountAsync(t => t.CreatedAt >= firstDayOfPreviousMonth && t.CreatedAt < firstDayOfCurrentMonth &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting));

        int CalcTrend(int current, int previous) =>
            previous == 0 ? 100 : (int)Math.Round((double)(current - previous) / previous * 100);

        return Ok(new DashboardStatsDto(
            totalAccounts,
            totalContacts,
            totalOpportunities,
            openCases,
            pipelineValue,
            wonDealsThisMonth,
            conversionRate,
            CalcTrend(currentAccounts, previousAccounts),
            CalcTrend(currentContacts, previousContacts),
            CalcTrend(currentOpportunities, previousOpportunities),
            CalcTrend(currentCases, previousCases)
        ));
    }

    [HttpGet("insight")]
    public async Task<IActionResult> GetInsight()
    {
        var now = DateTime.UtcNow;
        var firstDayOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstDayOfPreviousMonth = firstDayOfCurrentMonth.AddMonths(-1);

        var totalAccounts = await _dbContext.Companies.CountAsync();
        var totalContacts = await _dbContext.Contacts.CountAsync();
        var totalOpportunities = await _dbContext.Deals.CountAsync();
        var openCases = await _dbContext.Tickets.CountAsync(t =>
            t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting);

        var deals = await _dbContext.Deals.ToListAsync();
        var pipelineValue = deals.Where(d => d.Status == OpportunityStatus.Relevant).Sum(d => d.Amount.Amount);
        var wonDealsThisMonth = deals.Count(d =>
            d.ActualCloseDate.HasValue &&
            d.ActualCloseDate.Value.Year == now.Year &&
            d.ActualCloseDate.Value.Month == now.Month &&
            d.Status == OpportunityStatus.RealizedRevenue);
        var realizedTotal = deals.Count(d => d.Status == OpportunityStatus.RealizedRevenue);
        var notRelevantTotal = deals.Count(d => d.Status == OpportunityStatus.NotRelevant);
        var closedTotal = realizedTotal + notRelevantTotal;
        var conversionRate = closedTotal > 0
            ? (int)Math.Round((double)realizedTotal / closedTotal * 100)
            : 0;

        int CalcTrend(int current, int previous) =>
            previous == 0 ? 100 : (int)Math.Round((double)(current - previous) / previous * 100);

        var curAccounts = await _dbContext.Companies.CountAsync(c => c.CreatedAt >= firstDayOfCurrentMonth);
        var prevAccounts = await _dbContext.Companies.CountAsync(c => c.CreatedAt >= firstDayOfPreviousMonth && c.CreatedAt < firstDayOfCurrentMonth);
        var curContacts = await _dbContext.Contacts.CountAsync(c => c.CreatedAt >= firstDayOfCurrentMonth);
        var prevContacts = await _dbContext.Contacts.CountAsync(c => c.CreatedAt >= firstDayOfPreviousMonth && c.CreatedAt < firstDayOfCurrentMonth);
        var curOpportunities = await _dbContext.Deals.CountAsync(d => d.CreatedAt >= firstDayOfCurrentMonth);
        var prevOpportunities = await _dbContext.Deals.CountAsync(d => d.CreatedAt >= firstDayOfPreviousMonth && d.CreatedAt < firstDayOfCurrentMonth);
        var curCases = await _dbContext.Tickets.CountAsync(t => t.CreatedAt >= firstDayOfCurrentMonth &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting));
        var prevCases = await _dbContext.Tickets.CountAsync(t => t.CreatedAt >= firstDayOfPreviousMonth && t.CreatedAt < firstDayOfCurrentMonth &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Waiting));

        var ctx = new DashboardContext(
            totalAccounts, totalContacts, totalOpportunities, openCases,
            pipelineValue, wonDealsThisMonth, conversionRate,
            CalcTrend(curAccounts, prevAccounts),
            CalcTrend(curContacts, prevContacts),
            CalcTrend(curOpportunities, prevOpportunities),
            CalcTrend(curCases, prevCases)
        );

        var insight = await _aiService.GenerateDashboardInsightAsync(ctx);
        return Ok(new { insight });
    }

    [HttpGet("pipeline-chart")]
    public async Task<ActionResult<IEnumerable<PipelineChartItemDto>>> GetPipelineChart()
    {
        var deals = await _dbContext.Deals.ToListAsync();

        var result = deals
            .GroupBy(d => d.Status)
            .Select(g => new PipelineChartItemDto(
                g.Key.ToString(),
                g.Sum(d => d.Amount.Amount),
                g.Count()
            ))
            .ToList();

        return Ok(result);
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivity()
    {
        var activities = new List<RecentActivityDto>();

        var recentAccounts = await _dbContext.Companies
            .OrderByDescending(c => c.CreatedAt)
            .Take(3)
            .Select(c => new { c.Name, c.CreatedAt })
            .ToListAsync();

        activities.AddRange(recentAccounts.Select(a => new RecentActivityDto(
            "account",
            "New Account Created",
            $"{a.Name} added",
            a.CreatedAt
        )));

        var recentContacts = await _dbContext.Contacts
            .OrderByDescending(c => c.CreatedAt)
            .Take(3)
            .Select(c => new { c.FirstName, c.LastName, c.CreatedAt })
            .ToListAsync();

        activities.AddRange(recentContacts.Select(c => new RecentActivityDto(
            "contact",
            "New Contact Added",
            $"{c.FirstName} {c.LastName} added",
            c.CreatedAt
        )));

        var recentDeals = await _dbContext.Deals
            .OrderByDescending(d => d.CreatedAt)
            .Take(3)
            .Select(d => new { d.Name, d.CreatedAt, d.Status })
            .ToListAsync();

        activities.AddRange(recentDeals.Select(d => new RecentActivityDto(
            "opportunity",
            "Opportunity Created",
            $"{d.Name} added",
            d.CreatedAt
        )));

        var recentTickets = await _dbContext.Tickets
            .OrderByDescending(t => t.CreatedAt)
            .Take(3)
            .Select(t => new { t.TicketName, t.CreatedAt })
            .ToListAsync();

        activities.AddRange(recentTickets.Select(t => new RecentActivityDto(
            "case",
            "Support Case Opened",
            t.TicketName,
            t.CreatedAt
        )));

        var sorted = activities
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .ToList();

        return Ok(sorted);
    }
}

public record DashboardStatsDto(
    int TotalAccounts,
    int TotalContacts,
    int TotalOpportunities,
    int OpenCases,
    decimal PipelineValue,
    int WonDealsThisMonth,
    int ConversionRate,
    int TrendAccounts,
    int TrendContacts,
    int TrendOpportunities,
    int TrendCases
);

public record PipelineChartItemDto(string Name, decimal Value, int Count);

public record RecentActivityDto(string Type, string Title, string Description, DateTime CreatedAt);

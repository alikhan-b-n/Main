using Lama.Application.Common;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Queries;

public record GetLeadsQuery(LeadFilter Filter) : IQuery<PagedResult<LeadListItemDto>>;

public class GetLeadsQueryHandler : IQueryHandler<GetLeadsQuery, PagedResult<LeadListItemDto>>
{
    public const int MaxPageSize = 100;

    private readonly ILeadRepository _leads;

    public GetLeadsQueryHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task<PagedResult<LeadListItemDto>> Handle(GetLeadsQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter with
        {
            Page = Math.Max(1, query.Filter.Page),
            PageSize = Math.Clamp(query.Filter.PageSize, 1, MaxPageSize)
        };

        var page = await _leads.SearchAsync(filter, cancellationToken);

        return new PagedResult<LeadListItemDto>(
            page.Items.Select(l => l.ToListItem()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}

public record GetLeadByIdQuery(Guid Id) : IQuery<LeadDetailsDto?>;

public class GetLeadByIdQueryHandler : IQueryHandler<GetLeadByIdQuery, LeadDetailsDto?>
{
    private readonly ILeadRepository _leads;

    public GetLeadByIdQueryHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task<LeadDetailsDto?> Handle(GetLeadByIdQuery query, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(query.Id, cancellationToken);
        return lead?.ToDetails();
    }
}

public record GetLeadStatsQuery : IQuery<LeadStatsDto>;

public record LeadStatsDto(
    int Total,
    int Open,
    int NewThisWeek,
    int NewPreviousWeek,
    int HotOpen,
    int ContractsSigned,
    int ConversionRate,
    IReadOnlyList<CountItem> ByStatus,
    IReadOnlyList<CountItem> ByTemperature,
    IReadOnlyList<CountItem> BySource,
    IReadOnlyList<CountItem> ByBudget,
    IReadOnlyList<CountItem> ByDegree,
    IReadOnlyList<CountItem> ByCountry
);

public record CountItem(string Key, int Count);

public class GetLeadStatsQueryHandler : IQueryHandler<GetLeadStatsQuery, LeadStatsDto>
{
    private readonly ILeadRepository _leads;
    private readonly TimeProvider _clock;

    public GetLeadStatsQueryHandler(ILeadRepository leads, TimeProvider clock)
    {
        _leads = leads;
        _clock = clock;
    }

    public async Task<LeadStatsDto> Handle(GetLeadStatsQuery query, CancellationToken cancellationToken)
    {
        var rows = await _leads.GetStatsRowsAsync(cancellationToken);
        return LeadStatsCalculator.Calculate(rows, _clock.GetUtcNow().UtcDateTime);
    }
}

public static class LeadStatsCalculator
{
    public static LeadStatsDto Calculate(IReadOnlyList<LeadStatsRow> rows, DateTime nowUtc)
    {
        var weekAgo = nowUtc.AddDays(-7);
        var twoWeeksAgo = nowUtc.AddDays(-14);

        var open = rows.Where(r => r.Status is not (LeadStatus.ContractSigned or LeadStatus.Lost)).ToList();
        var signed = rows.Count(r => r.Status == LeadStatus.ContractSigned);
        var lost = rows.Count(r => r.Status == LeadStatus.Lost);
        var closed = signed + lost;

        return new LeadStatsDto(
            Total: rows.Count,
            Open: open.Count,
            NewThisWeek: rows.Count(r => r.CreatedAt >= weekAgo),
            NewPreviousWeek: rows.Count(r => r.CreatedAt >= twoWeeksAgo && r.CreatedAt < weekAgo),
            HotOpen: open.Count(r => r.Temperature == LeadTemperature.Hot),
            ContractsSigned: signed,
            ConversionRate: closed > 0 ? (int)Math.Round(100.0 * signed / closed) : 0,
            // Funnel and temperature keep every bucket, including empty ones, in a fixed order.
            ByStatus: Enum.GetValues<LeadStatus>()
                .Select(s => new CountItem(s.ToString(), rows.Count(r => r.Status == s)))
                .ToList(),
            ByTemperature: Enum.GetValues<LeadTemperature>()
                .Reverse()
                .Select(t => new CountItem(t.ToString(), rows.Count(r => r.Temperature == t)))
                .ToList(),
            BySource: CountBy(rows.Select(r => r.Source)),
            ByBudget: CountBy(rows.Select(r => r.AnnualBudget)),
            ByDegree: CountBy(rows.Select(r => r.TargetDegree)),
            ByCountry: CountBy(rows.SelectMany(r => r.TargetCountries))
        );
    }

    private static List<CountItem> CountBy(IEnumerable<string?> keys) =>
        keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .GroupBy(k => k!)
            .Select(g => new CountItem(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Key)
            .ToList();
}

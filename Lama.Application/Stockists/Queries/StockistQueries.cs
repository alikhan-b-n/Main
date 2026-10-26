using Lama.Application.Common;
using Lama.Domain.Stockists;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Queries;

/// <summary>Which part of the list a page shows: stores that carry books, or leads.</summary>
public enum StockistGroup
{
    All,
    Stores,
    Opportunities
}

public record StockistFilter(
    StockistGroup Group = StockistGroup.All,
    StockistStage? Stage = null,
    string? Title = null,
    string? Region = null,
    string? Search = null,
    bool NeedsAttention = false
);

public record GetStockistsQuery(StockistFilter Filter) : IQuery<IReadOnlyList<StockistListItemDto>>;

public class GetStockistsQueryHandler : IQueryHandler<GetStockistsQuery, IReadOnlyList<StockistListItemDto>>
{
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public GetStockistsQueryHandler(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StockistListItemDto>> Handle(GetStockistsQuery query, CancellationToken cancellationToken)
    {
        var today = StockistClock.Today(_clock);
        var all = await _stockists.ListAsync(cancellationToken);
        return StockistListFilter.Apply(all, query.Filter, today)
            .Select(s => s.ToListItem(today))
            .ToList();
    }
}

public static class StockistListFilter
{
    public static IEnumerable<Stockist> Apply(IEnumerable<Stockist> stockists, StockistFilter filter, DateOnly today)
    {
        var result = stockists;

        result = filter.Group switch
        {
            StockistGroup.Stores => result.Where(s => s.IsStore),
            StockistGroup.Opportunities => result.Where(s => !s.IsStore),
            _ => result
        };

        if (filter.Stage.HasValue)
            result = result.Where(s => s.Stage == filter.Stage.Value);

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim().ToLowerInvariant();
            result = result.Where(s => s.Stock.Any(l => l.Title == title && l.Quantity > 0));
        }

        if (!string.IsNullOrWhiteSpace(filter.Region))
            result = result.Where(s => string.Equals(s.Region, filter.Region.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            result = result.Where(s =>
                Contains(s.Name, search) || Contains(s.ContactName, search) ||
                Contains(s.Email, search) || Contains(s.Phone, search) || Contains(s.Address, search));
        }

        if (filter.NeedsAttention)
            result = result.Where(s => StockistAttention.For(s, today).Count > 0);

        // Stores that need something first, then by name
        return result
            .OrderByDescending(s => StockistAttention.For(s, today).Count)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static bool Contains(string? value, string search) =>
        value != null && value.Contains(search, StringComparison.OrdinalIgnoreCase);
}

public record GetStockistByIdQuery(Guid Id) : IQuery<StockistDetailsDto?>;

public class GetStockistByIdQueryHandler : IQueryHandler<GetStockistByIdQuery, StockistDetailsDto?>
{
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public GetStockistByIdQueryHandler(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<StockistDetailsDto?> Handle(GetStockistByIdQuery query, CancellationToken cancellationToken)
    {
        var stockist = await _stockists.GetByIdAsync(query.Id, cancellationToken);
        return stockist?.ToDetails(StockistClock.Today(_clock));
    }
}

public record GetStockistSummaryQuery : IQuery<StockistSummaryDto>;

public class GetStockistSummaryQueryHandler : IQueryHandler<GetStockistSummaryQuery, StockistSummaryDto>
{
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public GetStockistSummaryQueryHandler(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<StockistSummaryDto> Handle(GetStockistSummaryQuery query, CancellationToken cancellationToken)
    {
        var all = await _stockists.ListAsync(cancellationToken);
        return StockistSummaryCalculator.Calculate(all, StockistClock.Today(_clock));
    }
}

public static class StockistSummaryCalculator
{
    public static StockistSummaryDto Calculate(IReadOnlyList<Stockist> stockists, DateOnly today)
    {
        // Books with a store that stopped are still out there, so they count too
        var stores = stockists.Where(s => s.IsStore).ToList();
        var active = stores.Where(s => s.Stage == StockistStage.Active).ToList();
        var alerts = active.Select(s => StockistAttention.For(s, today)).ToList();
        var unpaid = stores.SelectMany(s => s.Invoices).Where(i => !i.IsPaid).ToList();

        return new StockistSummaryDto(
            ActiveStores: active.Count,
            BooksOnConsignment: stores.Sum(s => s.TotalBooks),
            NeedAttention: alerts.Count(a => a.Count > 0),
            LowStock: alerts.Count(a => a.Contains(StockistAlert.LowStock)),
            NoRecentContact: alerts.Count(a => a.Contains(StockistAlert.NoRecentContact)),
            UnpaidInvoices: unpaid.Count,
            OverdueInvoices: unpaid.Count(i => i.SentOn.AddDays(StockistAttention.OverdueInvoiceDays) < today),
            UnpaidAmount: unpaid.Sum(i => i.Amount ?? 0),
            OpenOpportunities: stockists.Count(s => !s.IsStore),
            ByTitle: BookTitles.Codes
                .Select(code => new TitleCount(
                    code,
                    stores.Sum(s => s.Stock.Where(l => l.Title == code).Sum(l => l.Quantity)),
                    stores.Count(s => s.Stock.Any(l => l.Title == code && l.Quantity > 0))))
                .ToList());
    }
}

internal static class StockistClock
{
    // Nadine works in Ireland; UTC is at most an hour off, which never moves a 60/90-day rule
    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}

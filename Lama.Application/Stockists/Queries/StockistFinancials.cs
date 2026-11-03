using Lama.Application.Common;
using Lama.Application.OnlineSales;
using Lama.Domain.OnlineSales.Entities;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Queries;

/// <summary>
/// Sales as the invoices tell them: what was invoiced in a year, how much of it is paid
/// and what is still owed. An invoice belongs to the year (and month) it was sent in.
/// Invoices typed in without an amount are counted but cannot add to the totals.
/// Website orders copied from Shopify sit next to them as online sales, by the day placed;
/// they are paid at checkout, so they never add to what is outstanding.
/// </summary>
public record GetStockistFinancialsQuery(int? Year = null) : IQuery<StockistFinancialsDto>;

public record StockistFinancialsDto(
    int Year,
    // Years that have invoices, newest first; always includes the current year
    IReadOnlyList<int> Years,
    MoneyTotals Totals,
    int InvoiceCount,
    int WithoutAmount,
    IReadOnlyList<MonthTotals> ByMonth,
    IReadOnlyList<StoreTotals> ByStore,
    IReadOnlyList<YearTotals> ByYear,
    OnlineTotals Online
);

public record MoneyTotals(decimal Invoiced, decimal Paid, decimal Outstanding);

/// <summary>Online is website sales; Invoiced and Paid are the store invoices.</summary>
public record MonthTotals(int Month, decimal Invoiced, decimal Paid, decimal Online = 0);

public record OnlineTotals(decimal Sales, int OrderCount);

public record StoreTotals(Guid Id, string Name, decimal Invoiced, decimal Paid, decimal Outstanding, int InvoiceCount);

public record YearTotals(
    int Year, decimal Invoiced, decimal Paid, decimal Outstanding, int InvoiceCount, decimal Online = 0, int OnlineOrderCount = 0);

public class GetStockistFinancialsQueryHandler : IQueryHandler<GetStockistFinancialsQuery, StockistFinancialsDto>
{
    private readonly IStockistRepository _stockists;
    private readonly IOnlineSalesRepository _online;
    private readonly TimeProvider _clock;

    public GetStockistFinancialsQueryHandler(IStockistRepository stockists, IOnlineSalesRepository online, TimeProvider clock)
    {
        _stockists = stockists;
        _online = online;
        _clock = clock;
    }

    public async Task<StockistFinancialsDto> Handle(GetStockistFinancialsQuery query, CancellationToken cancellationToken)
    {
        var all = await _stockists.ListAsync(cancellationToken);
        var orders = await _online.ListOrdersAsync(cancellationToken);
        return StockistFinancialsCalculator.Calculate(all, query.Year, StockistClock.Today(_clock), orders);
    }
}

public static class StockistFinancialsCalculator
{
    public static StockistFinancialsDto Calculate(
        IReadOnlyList<Stockist> stockists, int? year, DateOnly today, IReadOnlyList<OnlineOrder>? onlineOrders = null)
    {
        var invoices = stockists
            .SelectMany(s => s.Invoices.Select(i => (Store: s, Invoice: i)))
            .ToList();
        var orders = (onlineOrders ?? Array.Empty<OnlineOrder>()).Where(o => o.Counts).ToList();

        var selected = year ?? today.Year;
        var inYear = invoices.Where(x => x.Invoice.SentOn.Year == selected).ToList();
        var ordersInYear = orders.Where(o => o.PlacedOn.Year == selected).ToList();

        return new StockistFinancialsDto(
            Year: selected,
            Years: invoices.Select(x => x.Invoice.SentOn.Year)
                .Concat(orders.Select(o => o.PlacedOn.Year))
                .Append(today.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList(),
            Totals: Totals(inYear.Select(x => x.Invoice)),
            InvoiceCount: inYear.Count,
            WithoutAmount: inYear.Count(x => x.Invoice.Amount == null),
            ByMonth: Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var monthly = inYear.Where(x => x.Invoice.SentOn.Month == month).Select(x => x.Invoice).ToList();
                    var online = ordersInYear.Where(o => o.PlacedOn.Month == month).Sum(o => o.Amount);
                    return new MonthTotals(month, Sum(monthly), Sum(monthly.Where(i => i.IsPaid)), online);
                })
                .ToList(),
            ByStore: inYear
                .GroupBy(x => x.Store.Id)
                .Select(g =>
                {
                    var totals = Totals(g.Select(x => x.Invoice));
                    return new StoreTotals(g.Key, g.First().Store.Name, totals.Invoiced, totals.Paid, totals.Outstanding, g.Count());
                })
                .OrderByDescending(s => s.Invoiced)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ByYear: invoices.Select(x => x.Invoice.SentOn.Year)
                .Concat(orders.Select(o => o.PlacedOn.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y =>
                {
                    var yearInvoices = invoices.Where(x => x.Invoice.SentOn.Year == y).Select(x => x.Invoice).ToList();
                    var yearOrders = orders.Where(o => o.PlacedOn.Year == y).ToList();
                    var totals = Totals(yearInvoices);
                    return new YearTotals(y, totals.Invoiced, totals.Paid, totals.Outstanding, yearInvoices.Count,
                        yearOrders.Sum(o => o.Amount), yearOrders.Count);
                })
                .ToList(),
            Online: new OnlineTotals(ordersInYear.Sum(o => o.Amount), ordersInYear.Count));
    }

    private static MoneyTotals Totals(IEnumerable<StockistInvoice> invoices)
    {
        var list = invoices.ToList();
        var invoiced = Sum(list);
        var paid = Sum(list.Where(i => i.IsPaid));
        return new MoneyTotals(invoiced, paid, invoiced - paid);
    }

    private static decimal Sum(IEnumerable<StockistInvoice> invoices) => invoices.Sum(i => i.Amount ?? 0);
}

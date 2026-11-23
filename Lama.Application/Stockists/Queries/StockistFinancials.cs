using Lama.Application.Common;
using Lama.Application.Expenses;
using Lama.Application.OnlineSales;
using Lama.Domain.Expenses.Entities;
using Lama.Domain.OnlineSales.Entities;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Queries;

/// <summary>
/// Sales as the invoices tell them: what was invoiced in a year, how much of it is paid
/// and what is still owed. An invoice belongs to the year (and month) it was sent in.
/// Invoices typed in without an amount are counted but cannot add to the totals.
/// Website orders copied from Shopify sit next to them as online sales, by the day placed;
/// they are paid at checkout, so they never add to what is outstanding.
/// Expenses (shipping, Shopify, the CRM) are set against both: what is left is the sales
/// minus the expenses of the same months.
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
    OnlineTotals Online,
    ExpenseTotals Expenses
);

public record MoneyTotals(decimal Invoiced, decimal Paid, decimal Outstanding);

/// <summary>Online is website sales; Invoiced and Paid are the store invoices.</summary>
public record MonthTotals(int Month, decimal Invoiced, decimal Paid, decimal Online = 0, decimal Expenses = 0);

public record OnlineTotals(decimal Sales, int OrderCount);

/// <summary>Spent in the year, by category. Net is store invoices plus website sales, minus the total.</summary>
public record ExpenseTotals(decimal Total, decimal Shipping, decimal Shopify, decimal Crm, decimal Other, decimal Net);

public record StoreTotals(Guid Id, string Name, decimal Invoiced, decimal Paid, decimal Outstanding, int InvoiceCount);

public record YearTotals(
    int Year, decimal Invoiced, decimal Paid, decimal Outstanding, int InvoiceCount, decimal Online = 0, int OnlineOrderCount = 0,
    decimal Expenses = 0);

public class GetStockistFinancialsQueryHandler : IQueryHandler<GetStockistFinancialsQuery, StockistFinancialsDto>
{
    private readonly IStockistRepository _stockists;
    private readonly IOnlineSalesRepository _online;
    private readonly IExpenseRepository _expenses;
    private readonly TimeProvider _clock;

    public GetStockistFinancialsQueryHandler(
        IStockistRepository stockists, IOnlineSalesRepository online, IExpenseRepository expenses, TimeProvider clock)
    {
        _stockists = stockists;
        _online = online;
        _expenses = expenses;
        _clock = clock;
    }

    public async Task<StockistFinancialsDto> Handle(GetStockistFinancialsQuery query, CancellationToken cancellationToken)
    {
        var all = await _stockists.ListAsync(cancellationToken);
        var orders = await _online.ListOrdersAsync(cancellationToken);
        var expenses = await _expenses.ListAsync(cancellationToken);
        return StockistFinancialsCalculator.Calculate(all, query.Year, StockistClock.Today(_clock), orders, expenses);
    }
}

public static class StockistFinancialsCalculator
{
    public static StockistFinancialsDto Calculate(
        IReadOnlyList<Stockist> stockists, int? year, DateOnly today, IReadOnlyList<OnlineOrder>? onlineOrders = null,
        IReadOnlyList<Expense>? expenses = null)
    {
        var spent = expenses ?? Array.Empty<Expense>();
        var invoices = stockists
            .SelectMany(s => s.Invoices.Select(i => (Store: s, Invoice: i)))
            .ToList();
        var orders = (onlineOrders ?? Array.Empty<OnlineOrder>()).Where(o => o.Counts).ToList();

        var selected = year ?? today.Year;
        var inYear = invoices.Where(x => x.Invoice.SentOn.Year == selected).ToList();
        var ordersInYear = orders.Where(o => o.PlacedOn.Year == selected).ToList();
        // A monthly charge runs on into years with no sales yet; only the years it started in are listed
        var years = invoices.Select(x => x.Invoice.SentOn.Year)
            .Concat(orders.Select(o => o.PlacedOn.Year))
            .Concat(spent.Select(e => e.StartsOn.Year))
            .Distinct()
            .ToList();
        var totals = Totals(inYear.Select(x => x.Invoice));
        var online = ordersInYear.Sum(o => o.Amount);
        decimal SpentOn(ExpenseCategory category) => spent.Where(e => e.Category == category).Sum(e => e.AmountIn(selected, today));
        var spentInYear = spent.Sum(e => e.AmountIn(selected, today));

        return new StockistFinancialsDto(
            Year: selected,
            Years: years
                .Append(today.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList(),
            Totals: totals,
            InvoiceCount: inYear.Count,
            WithoutAmount: inYear.Count(x => x.Invoice.Amount == null),
            ByMonth: Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var monthly = inYear.Where(x => x.Invoice.SentOn.Month == month).Select(x => x.Invoice).ToList();
                    var monthOnline = ordersInYear.Where(o => o.PlacedOn.Month == month).Sum(o => o.Amount);
                    return new MonthTotals(month, Sum(monthly), Sum(monthly.Where(i => i.IsPaid)), monthOnline,
                        spent.Sum(e => e.AmountIn(selected, month, today)));
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
            ByYear: years
                .OrderByDescending(y => y)
                .Select(y =>
                {
                    var yearInvoices = invoices.Where(x => x.Invoice.SentOn.Year == y).Select(x => x.Invoice).ToList();
                    var yearOrders = orders.Where(o => o.PlacedOn.Year == y).ToList();
                    var yearTotals = Totals(yearInvoices);
                    return new YearTotals(y, yearTotals.Invoiced, yearTotals.Paid, yearTotals.Outstanding, yearInvoices.Count,
                        yearOrders.Sum(o => o.Amount), yearOrders.Count, spent.Sum(e => e.AmountIn(y, today)));
                })
                .ToList(),
            Online: new OnlineTotals(online, ordersInYear.Count),
            Expenses: new ExpenseTotals(spentInYear, SpentOn(ExpenseCategory.Shipping), SpentOn(ExpenseCategory.Shopify),
                SpentOn(ExpenseCategory.Crm), SpentOn(ExpenseCategory.Other), totals.Invoiced + online - spentInYear));
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

using Lama.Application.Stockists.Queries;
using Lama.Domain.Stockists.Entities;

namespace Lama.Tests.Stockists;

public class StockistFinancialsTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    private static Stockist Store(string name) =>
        Stockist.Create(new StockistDetails(name), StockistStage.Active, Today);

    [Fact]
    public void TotalsTheSelectedYearByMonthAndStore()
    {
        var kinsale = Store("Bookstór, Kinsale");
        var paid = kinsale.AddInvoice(new DateOnly(2026, 3, 4), 100m, "INV-1");
        kinsale.MarkInvoicePaid(paid.Id, new DateOnly(2026, 4, 1));
        kinsale.AddInvoice(new DateOnly(2026, 3, 20), 50m, "INV-2");
        var galway = Store("Kennys, Galway");
        galway.AddInvoice(new DateOnly(2026, 7, 1), 200m, null);
        galway.AddInvoice(new DateOnly(2025, 12, 1), 80m, null);

        var result = StockistFinancialsCalculator.Calculate(new[] { kinsale, galway }, 2026, Today);

        Assert.Equal(new MoneyTotals(350m, 100m, 250m), result.Totals);
        Assert.Equal(3, result.InvoiceCount);
        Assert.Equal(12, result.ByMonth.Count);
        Assert.Equal(new MonthTotals(3, 150m, 100m), result.ByMonth[2]);
        Assert.Equal(new MonthTotals(7, 200m, 0m), result.ByMonth[6]);
        Assert.Equal(new[] { "Kennys, Galway", "Bookstór, Kinsale" }, result.ByStore.Select(s => s.Name));
        Assert.Equal(new[] { 2026, 2025 }, result.Years);
        Assert.Equal(80m, result.ByYear.Single(y => y.Year == 2025).Invoiced);
    }

    [Fact]
    public void CountsInvoicesWithoutAnAmountSeparately()
    {
        var store = Store("Charlie Binds Galway");
        store.AddInvoice(new DateOnly(2025, 6, 14), null, "From Excel");

        var result = StockistFinancialsCalculator.Calculate(new[] { store }, 2025, Today);

        Assert.Equal(1, result.InvoiceCount);
        Assert.Equal(1, result.WithoutAmount);
        Assert.Equal(0m, result.Totals.Invoiced);
    }

    [Fact]
    public void DefaultsToThisYearAndAlwaysOffersIt()
    {
        var result = StockistFinancialsCalculator.Calculate(Array.Empty<Stockist>(), null, Today);

        Assert.Equal(2026, result.Year);
        Assert.Equal(new[] { 2026 }, result.Years);
        Assert.Empty(result.ByStore);
    }
}

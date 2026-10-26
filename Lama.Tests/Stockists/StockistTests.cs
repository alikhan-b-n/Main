using Lama.Application.Stockists.Commands;
using Lama.Application.Stockists.Queries;
using Lama.Domain.Stockists;
using Lama.Domain.Stockists.Entities;

namespace Lama.Tests.Stockists;

public class StockistTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    private static Stockist Store(StockistStage stage = StockistStage.Active) =>
        Stockist.Create(new StockistDetails("Bookstór, Kinsale", "Ireland"), stage, Today);

    [Fact]
    public void Delivery_AddsToStockAndCountsAsContact()
    {
        var store = Store();

        store.RecordDelivery("Cork", 10, Today.AddDays(-5));
        store.RecordDelivery("cork", 5, Today);

        Assert.Equal(15, store.QuantityOf("cork"));
        Assert.Equal(Today, store.LastContactedOn);
        Assert.Equal(2, store.Events.Count(e => e.Kind == StockistEventKind.Delivery));
    }

    [Fact]
    public void Delivery_ToALead_MakesItAnActiveStore()
    {
        var lead = Store(StockistStage.Interested);

        lead.RecordDelivery("kerry", 5, Today);

        Assert.Equal(StockistStage.Active, lead.Stage);
        Assert.Contains(lead.Events, e => e.Kind == StockistEventKind.StageChanged && e.ToStage == StockistStage.Active);
    }

    [Fact]
    public void StockCount_SetsQuantityAndKeepsThePreviousOne()
    {
        var store = Store();
        store.RecordDelivery("galway", 10, Today.AddDays(-30));

        store.RecordStockCount("galway", 3, Today, "still 3 in stock");

        Assert.Equal(3, store.QuantityOf("galway"));
        var count = store.Events.Single(e => e.Kind == StockistEventKind.StockCount);
        Assert.Equal(10, count.PreviousQuantity);
        Assert.Equal(3, count.Quantity);
    }

    [Fact]
    public void Return_CannotTakeBackMoreThanTheStoreHas()
    {
        var store = Store();
        store.RecordDelivery("berlin", 2, Today);

        Assert.Throws<ArgumentException>(() => store.RecordReturn("berlin", 3, Today));
        store.RecordReturn("berlin", 2, Today);
        Assert.Equal(0, store.QuantityOf("berlin"));
    }

    [Fact]
    public void UnknownTitle_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Store().RecordDelivery("dublin", 1, Today));
    }

    [Fact]
    public void BackDatedContact_DoesNotMoveLastContactBackwards()
    {
        var store = Store();
        store.LogContact("Called", Today);

        store.LogContact("Visited in summer", Today.AddDays(-100));

        Assert.Equal(Today, store.LastContactedOn);
    }

    [Fact]
    public void Note_IsNotAContact()
    {
        var store = Store();

        store.AddNote("Prefers email", Today);

        Assert.Null(store.LastContactedOn);
    }

    [Fact]
    public void Invoice_CanBePaidAndUndone()
    {
        var store = Store();
        var invoice = store.AddInvoice(Today.AddDays(-10), 120m, "INV-7");

        store.MarkInvoicePaid(invoice.Id, Today);
        Assert.True(invoice.IsPaid);

        store.MarkInvoiceUnpaid(invoice.Id);
        Assert.False(invoice.IsPaid);
    }

    [Fact]
    public void Attention_FlagsLowStockOldContactAndOverdueInvoice()
    {
        var store = Store();
        store.RecordDelivery("cork", 4, Today.AddDays(-91));
        store.AddInvoice(Today.AddDays(-61), 50m, null);

        var alerts = StockistAttention.For(store, Today);

        Assert.Equal(
            new[] { StockistAlert.LowStock, StockistAlert.NoRecentContact, StockistAlert.OverdueInvoice },
            alerts);
    }

    [Fact]
    public void Attention_HealthyStoreHasNoAlerts()
    {
        var store = Store();
        store.RecordDelivery("cork", 5, Today.AddDays(-90));
        store.AddInvoice(Today.AddDays(-60), 50m, null);

        Assert.Empty(StockistAttention.For(store, Today));
    }

    [Fact]
    public void Attention_IgnoresLeads()
    {
        Assert.Empty(StockistAttention.For(Store(StockistStage.Lead), Today));
    }

    [Fact]
    public void Filter_ByTitleGroupAndAttention()
    {
        var cork = Store();
        cork.RecordDelivery("cork", 10, Today);
        var galway = Stockist.Create(new StockistDetails("Kennys, Galway"), StockistStage.Active, Today);
        galway.RecordDelivery("galway", 2, Today);
        var lead = Stockist.Create(new StockistDetails("She Said"), StockistStage.Lead, Today);
        var all = new[] { cork, galway, lead };

        Assert.Equal(new[] { cork }, StockistListFilter.Apply(all, new StockistFilter(Title: "Cork"), Today));
        Assert.Equal(new[] { lead }, StockistListFilter.Apply(all, new StockistFilter(StockistGroup.Opportunities), Today));
        Assert.Equal(new[] { galway }, StockistListFilter.Apply(all, new StockistFilter(NeedsAttention: true), Today));
        Assert.Equal(new[] { galway }, StockistListFilter.Apply(all, new StockistFilter(Search: "kenny"), Today));
    }

    [Fact]
    public void Summary_CountsBooksPerTitleAndUnpaidInvoices()
    {
        var a = Store();
        a.RecordDelivery("cork", 10, Today);
        a.AddInvoice(Today.AddDays(-70), 80m, null);
        var b = Stockist.Create(new StockistDetails("Kennys"), StockistStage.Active, Today);
        b.RecordDelivery("cork", 3, Today);
        var lead = Stockist.Create(new StockistDetails("She Said"), StockistStage.Lead, Today);

        var summary = StockistSummaryCalculator.Calculate(new[] { a, b, lead }, Today);

        Assert.Equal(2, summary.ActiveStores);
        Assert.Equal(13, summary.BooksOnConsignment);
        Assert.Equal(2, summary.NeedAttention);
        Assert.Equal(1, summary.OverdueInvoices);
        Assert.Equal(80m, summary.UnpaidAmount);
        Assert.Equal(1, summary.OpenOpportunities);
        var corkCount = summary.ByTitle.Single(t => t.Title == "cork");
        Assert.Equal(13, corkCount.Copies);
        Assert.Equal(2, corkCount.Stores);
        Assert.Equal(BookTitles.Codes.Count, summary.ByTitle.Count);
    }

    [Fact]
    public void Import_UsesTheLatestDeliveryAsLastContactAndKeepsUndatedStock()
    {
        var store = ImportStockistsCommandHandler.Build(new ImportedStockist(
            "Fermoy Books, Fermoy",
            Region: "Ireland",
            Stock: new[]
            {
                new ImportedStock("cork", 5, new DateOnly(2026, 3, 14)),
                new ImportedStock("galway", 1),
                new ImportedStock("kerry", 2, new DateOnly(2025, 12, 1))
            },
            Notes: new[] { "2 invoices unpaid" },
            Invoices: new[] { new ImportedInvoice(new DateOnly(2025, 8, 11)) }), Today);

        Assert.Equal(StockistStage.Active, store.Stage);
        Assert.Equal(8, store.TotalBooks);
        Assert.Equal(new DateOnly(2026, 3, 14), store.LastContactedOn);
        Assert.Single(store.Invoices);
        Assert.Contains(store.Events, e => e.Kind == StockistEventKind.Note && e.Text == "2 invoices unpaid");
    }

    [Fact]
    public void Import_KeepsALeadWithoutBooksAsALead()
    {
        var lead = ImportStockistsCommandHandler.Build(
            new ImportedStockist("She Said", Region: "Germany", Stage: StockistStage.Lead), Today);

        Assert.Equal(StockistStage.Lead, lead.Stage);
        Assert.Null(lead.LastContactedOn);
    }
}

using Lama.Domain.Stockists.Entities;

namespace Lama.Domain.Stockists;

/// <summary>
/// When an active store turns red on the dashboard. The thresholds are what Nadine asked
/// for: 4 or fewer books left, no contact for 3+ months, an invoice unpaid for 2+ months.
/// </summary>
public static class StockistAttention
{
    public const int LowStockThreshold = 4;
    public const int NoContactDays = 90;
    public const int OverdueInvoiceDays = 60;

    public static IReadOnlyList<StockistAlert> For(Stockist stockist, DateOnly today)
    {
        if (stockist.Stage != StockistStage.Active)
            return Array.Empty<StockistAlert>();

        var alerts = new List<StockistAlert>();

        if (stockist.TotalBooks <= LowStockThreshold)
            alerts.Add(StockistAlert.LowStock);

        if (stockist.LastContactedOn == null || stockist.LastContactedOn.Value.AddDays(NoContactDays) < today)
            alerts.Add(StockistAlert.NoRecentContact);

        if (stockist.Invoices.Any(i => !i.IsPaid && i.SentOn.AddDays(OverdueInvoiceDays) < today))
            alerts.Add(StockistAlert.OverdueInvoice);

        return alerts;
    }
}

public enum StockistAlert
{
    LowStock,
    NoRecentContact,
    OverdueInvoice
}

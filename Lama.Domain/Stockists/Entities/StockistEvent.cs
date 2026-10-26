using Lama.Domain.Common;

namespace Lama.Domain.Stockists.Entities;

/// <summary>
/// History entry of a store: deliveries, stock counts, contacts, invoices and stage changes.
/// <see cref="OccurredOn"/> is the day it happened, which can be earlier than
/// <see cref="Entity.CreatedAt"/> when something is written down later or imported.
/// </summary>
public class StockistEvent : Entity
{
    public const int MaxTextLength = 2000;

    public Guid StockistId { get; private set; }
    public StockistEventKind Kind { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Title { get; private set; }
    public int? Quantity { get; private set; }
    /// <summary>For a stock count: what the store had before, so the history shows the change.</summary>
    public int? PreviousQuantity { get; private set; }
    public decimal? Amount { get; private set; }
    public StockistStage? FromStage { get; private set; }
    public StockistStage? ToStage { get; private set; }
    public string? Text { get; private set; }

    private StockistEvent() { }

    private StockistEvent(Guid stockistId, StockistEventKind kind, DateOnly occurredOn)
    {
        StockistId = stockistId;
        Kind = kind;
        OccurredOn = occurredOn;
    }

    internal static StockistEvent Created(Guid stockistId, DateOnly on) =>
        new(stockistId, StockistEventKind.Created, on);

    internal static StockistEvent Delivered(Guid stockistId, DateOnly on, string title, int quantity, string? text) =>
        new(stockistId, StockistEventKind.Delivery, on) { Title = title, Quantity = quantity, Text = text };

    internal static StockistEvent Returned(Guid stockistId, DateOnly on, string title, int quantity, string? text) =>
        new(stockistId, StockistEventKind.Return, on) { Title = title, Quantity = quantity, Text = text };

    internal static StockistEvent Counted(Guid stockistId, DateOnly on, string title, int previous, int quantity, string? text) =>
        new(stockistId, StockistEventKind.StockCount, on)
        {
            Title = title, PreviousQuantity = previous, Quantity = quantity, Text = text
        };

    internal static StockistEvent Contacted(Guid stockistId, DateOnly on, string text) =>
        new(stockistId, StockistEventKind.Contact, on) { Text = text };

    internal static StockistEvent Note(Guid stockistId, DateOnly on, string text) =>
        new(stockistId, StockistEventKind.Note, on) { Text = text };

    internal static StockistEvent StageChanged(Guid stockistId, DateOnly on, StockistStage from, StockistStage to) =>
        new(stockistId, StockistEventKind.StageChanged, on) { FromStage = from, ToStage = to };

    internal static StockistEvent InvoiceSent(Guid stockistId, DateOnly on, decimal? amount, string? reference) =>
        new(stockistId, StockistEventKind.InvoiceSent, on) { Amount = amount, Text = reference };

    internal static StockistEvent InvoicePaid(Guid stockistId, DateOnly on, decimal? amount, string? reference) =>
        new(stockistId, StockistEventKind.InvoicePaid, on) { Amount = amount, Text = reference };
}

public enum StockistEventKind
{
    Created,
    Delivery,
    Return,
    StockCount,
    Contact,
    Note,
    StageChanged,
    InvoiceSent,
    InvoicePaid
}

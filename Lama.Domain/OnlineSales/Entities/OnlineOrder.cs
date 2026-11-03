using System.Text.RegularExpressions;
using Lama.Domain.Common;
using Lama.Domain.Stockists;

namespace Lama.Domain.OnlineSales.Entities;

/// <summary>
/// An order from the website, copied from Shopify so Financials can put online sales
/// next to the store invoices. Only what the totals need is kept: no customer details.
/// The amount is what the books sold for after discounts and refunds, without shipping,
/// so it compares fairly with an invoice to a store.
/// </summary>
public class OnlineOrder : AggregateRoot
{
    public const int MaxShopifyIdLength = 64;
    public const int MaxNumberLength = 32;
    public const int MaxProductTitleLength = 255;

    private readonly List<OnlineOrderLine> _lines = new();

    /// <summary>Shopify's id ("gid://shopify/Order/…"), which makes a sync safe to repeat.</summary>
    public string ShopifyId { get; private set; } = string.Empty;

    /// <summary>The number the bookseller sees in Shopify, e.g. "#1042".</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>The day the order was placed, in the store's own time zone.</summary>
    public DateOnly PlacedOn { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool Cancelled { get; private set; }

    /// <summary>When Shopify last changed the order (a refund, an edit).</summary>
    public DateTime ChangedAt { get; private set; }

    public IReadOnlyList<OnlineOrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Cancelled orders are kept, so a sync sees the change, but never counted.</summary>
    public bool Counts => !Cancelled;

    private OnlineOrder() { }

    public static OnlineOrder Create(string shopifyId, OnlineOrderSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(shopifyId))
            throw new ArgumentException("Shopify id is required", nameof(shopifyId));

        var order = new OnlineOrder { ShopifyId = shopifyId };
        order.Update(snapshot);
        return order;
    }

    /// <summary>Takes Shopify's current version of the order, replacing what was there.</summary>
    public void Update(OnlineOrderSnapshot snapshot)
    {
        if (snapshot.Amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(snapshot));

        Number = snapshot.Number.Length > MaxNumberLength ? snapshot.Number[..MaxNumberLength] : snapshot.Number;
        PlacedOn = snapshot.PlacedOn;
        Amount = snapshot.Amount;
        Currency = snapshot.Currency;
        Cancelled = snapshot.Cancelled;
        ChangedAt = snapshot.ChangedAt;

        // Rewritten in place rather than cleared: a line keeps its key, so EF updates the row
        var lines = snapshot.Lines.Where(l => l.Quantity > 0).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            var productTitle = lines[i].ProductTitle.Length > MaxProductTitleLength
                ? lines[i].ProductTitle[..MaxProductTitleLength]
                : lines[i].ProductTitle;
            if (i < _lines.Count)
                _lines[i].Change(productTitle, BookTitleOf(productTitle), lines[i].Quantity);
            else
                _lines.Add(new OnlineOrderLine(i + 1, productTitle, BookTitleOf(productTitle), lines[i].Quantity));
        }
        if (_lines.Count > lines.Count)
            _lines.RemoveRange(lines.Count, _lines.Count - lines.Count);

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The book a product is, by the place in its name ("Noah's Cork Adventure" is cork).
    /// A product naming several places (a bundle) or none is not one book, so it has none.
    /// </summary>
    public static string? BookTitleOf(string productTitle)
    {
        var matches = BookTitles.Codes
            .Where(code => Regex.IsMatch(productTitle, $@"\b{code}\b", RegexOptions.IgnoreCase))
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}

/// <summary>One product in an order. Title is a book code, or null for bundles and anything else.</summary>
public class OnlineOrderLine
{
    public int Position { get; private set; }
    public string ProductTitle { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public int Quantity { get; private set; }

    private OnlineOrderLine() { }

    internal OnlineOrderLine(int position, string productTitle, string? title, int quantity)
    {
        Position = position;
        Change(productTitle, title, quantity);
    }

    internal void Change(string productTitle, string? title, int quantity)
    {
        ProductTitle = productTitle;
        Title = title;
        Quantity = quantity;
    }
}

/// <summary>An order as Shopify describes it now.</summary>
public record OnlineOrderSnapshot(
    string Number,
    DateOnly PlacedOn,
    decimal Amount,
    string Currency,
    bool Cancelled,
    DateTime ChangedAt,
    IReadOnlyList<OnlineOrderLineSnapshot> Lines
);

public record OnlineOrderLineSnapshot(string ProductTitle, int Quantity);

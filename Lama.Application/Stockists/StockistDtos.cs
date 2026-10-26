using Lama.Domain.Stockists;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists;

public record StockistListItemDto(
    Guid Id,
    string Name,
    string? Region,
    string? ContactName,
    string? Email,
    string? Phone,
    string Stage,
    // Copies per title code; titles the store never had are left out
    IReadOnlyDictionary<string, int> Stock,
    int TotalBooks,
    DateOnly? LastContactedOn,
    int UnpaidInvoices,
    decimal UnpaidAmount,
    IReadOnlyList<string> Alerts,
    DateTime CreatedAt
);

public record StockistDetailsDto(
    Guid Id,
    string Name,
    string? Region,
    string? Address,
    string? ContactName,
    string? Email,
    string? Phone,
    string Stage,
    IReadOnlyDictionary<string, int> Stock,
    int TotalBooks,
    DateOnly? LastContactedOn,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<StockistInvoiceDto> Invoices,
    IReadOnlyList<StockistEventDto> Events,
    DateTime CreatedAt
);

public record StockistInvoiceDto(
    Guid Id,
    DateOnly SentOn,
    decimal? Amount,
    string? Reference,
    DateOnly? PaidOn,
    bool Overdue
);

public record StockistEventDto(
    Guid Id,
    string Kind,
    DateOnly OccurredOn,
    string? Title,
    int? Quantity,
    int? PreviousQuantity,
    decimal? Amount,
    string? FromStage,
    string? ToStage,
    string? Text,
    DateTime CreatedAt
);

public record StockistSummaryDto(
    int ActiveStores,
    int BooksOnConsignment,
    int NeedAttention,
    int LowStock,
    int NoRecentContact,
    int UnpaidInvoices,
    int OverdueInvoices,
    decimal UnpaidAmount,
    int OpenOpportunities,
    IReadOnlyList<TitleCount> ByTitle
);

public record TitleCount(string Title, int Copies, int Stores);

internal static class StockistMapping
{
    // Timestamps are stored as UTC in `timestamp without time zone` and come back with
    // Kind=Unspecified, which serializes without a "Z" and makes browsers read them as local time.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static IReadOnlyDictionary<string, int> StockMap(this Stockist stockist) =>
        stockist.Stock
            .OrderBy(s => BookTitlesOrder(s.Title))
            .ToDictionary(s => s.Title, s => s.Quantity);

    public static IReadOnlyList<string> AlertCodes(this Stockist stockist, DateOnly today) =>
        StockistAttention.For(stockist, today).Select(a => a.ToString()).ToList();

    public static StockistListItemDto ToListItem(this Stockist stockist, DateOnly today)
    {
        var unpaid = stockist.Invoices.Where(i => !i.IsPaid).ToList();
        return new StockistListItemDto(
            stockist.Id,
            stockist.Name,
            stockist.Region,
            stockist.ContactName,
            stockist.Email,
            stockist.Phone,
            stockist.Stage.ToString(),
            stockist.StockMap(),
            stockist.TotalBooks,
            stockist.LastContactedOn,
            unpaid.Count,
            unpaid.Sum(i => i.Amount ?? 0),
            stockist.AlertCodes(today),
            Utc(stockist.CreatedAt));
    }

    public static StockistDetailsDto ToDetails(this Stockist stockist, DateOnly today) => new(
        stockist.Id,
        stockist.Name,
        stockist.Region,
        stockist.Address,
        stockist.ContactName,
        stockist.Email,
        stockist.Phone,
        stockist.Stage.ToString(),
        stockist.StockMap(),
        stockist.TotalBooks,
        stockist.LastContactedOn,
        stockist.AlertCodes(today),
        stockist.Invoices
            .OrderBy(i => i.IsPaid)
            .ThenByDescending(i => i.SentOn)
            .Select(i => new StockistInvoiceDto(
                i.Id,
                i.SentOn,
                i.Amount,
                i.Reference,
                i.PaidOn,
                !i.IsPaid && i.SentOn.AddDays(StockistAttention.OverdueInvoiceDays) < today))
            .ToList(),
        stockist.Events
            .OrderByDescending(e => e.OccurredOn)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new StockistEventDto(
                e.Id,
                e.Kind.ToString(),
                e.OccurredOn,
                e.Title,
                e.Quantity,
                e.PreviousQuantity,
                e.Amount,
                e.FromStage?.ToString(),
                e.ToStage?.ToString(),
                e.Text,
                Utc(e.CreatedAt)))
            .ToList(),
        Utc(stockist.CreatedAt));

    private static int BookTitlesOrder(string code)
    {
        var index = BookTitles.Codes.ToList().IndexOf(code);
        return index < 0 ? int.MaxValue : index;
    }
}

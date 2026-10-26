using Lama.Application.Common;
using Lama.Application.Stockists.Queries;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Commands;

/// <summary>
/// Moves the stores over from the old Excel sheet. A store whose name is already in the
/// CRM is skipped, so running the same file twice adds nothing.
/// </summary>
public record ImportStockistsCommand(IReadOnlyList<ImportedStockist> Stockists) : ICommand<ImportResult>;

public record ImportedStockist(
    string Name,
    string? Region = null,
    string? Address = null,
    string? ContactName = null,
    string? Email = null,
    string? Phone = null,
    StockistStage Stage = StockistStage.Active,
    IReadOnlyList<ImportedStock>? Stock = null,
    IReadOnlyList<string>? Notes = null,
    IReadOnlyList<ImportedInvoice>? Invoices = null);

/// <summary>Copies of a title the store has; <paramref name="DeliveredOn"/> when the sheet had a date.</summary>
public record ImportedStock(string Title, int Quantity, DateOnly? DeliveredOn = null);

public record ImportedInvoice(DateOnly SentOn, decimal? Amount = null, string? Reference = null, DateOnly? PaidOn = null);

public record ImportResult(int Imported, IReadOnlyList<string> Skipped);

public class ImportStockistsCommandHandler : ICommandHandler<ImportStockistsCommand, ImportResult>
{
    public const string ImportNote = "Imported from the Excel sheet";

    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public ImportStockistsCommandHandler(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<ImportResult> Handle(ImportStockistsCommand command, CancellationToken cancellationToken)
    {
        var today = StockistClock.Today(_clock);
        var taken = new HashSet<string>(await _stockists.GetNamesAsync(cancellationToken));
        var created = new List<Stockist>();
        var skipped = new List<string>();

        foreach (var item in command.Stockists)
        {
            var key = item.Name.Trim().ToLowerInvariant();
            if (!taken.Add(key))
            {
                skipped.Add(item.Name.Trim());
                continue;
            }

            created.Add(Build(item, today));
        }

        await _stockists.AddRangeAsync(created, cancellationToken);
        return new ImportResult(created.Count, skipped);
    }

    public static Stockist Build(ImportedStockist item, DateOnly today)
    {
        var stockist = Stockist.Create(
            new StockistDetails(item.Name, item.Region, item.Address, item.ContactName, item.Email, item.Phone),
            item.Stage,
            today);

        // Dated deliveries in date order, so "last contacted" ends on the latest one
        foreach (var stock in (item.Stock ?? Array.Empty<ImportedStock>()).OrderBy(s => s.DeliveredOn))
        {
            if (stock.DeliveredOn is { } on && stock.Quantity > 0)
                stockist.RecordDelivery(stock.Title, stock.Quantity, on, ImportNote);
            else
                stockist.SetOpeningStock(stock.Title, stock.Quantity, today, ImportNote);
        }

        foreach (var note in item.Notes ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(note))
                stockist.AddNote(note, today);
        }

        foreach (var invoice in item.Invoices ?? Array.Empty<ImportedInvoice>())
        {
            var added = stockist.AddInvoice(invoice.SentOn, invoice.Amount, invoice.Reference);
            if (invoice.PaidOn is { } paidOn)
                stockist.MarkInvoicePaid(added.Id, paidOn);
        }

        // A delivery on a lead makes it Active; the sheet's own stage wins
        stockist.ChangeStage(item.Stage, today);
        return stockist;
    }
}

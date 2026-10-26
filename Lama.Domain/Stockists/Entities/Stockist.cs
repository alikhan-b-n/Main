using Lama.Domain.Common;

namespace Lama.Domain.Stockists.Entities;

/// <summary>
/// A shop, café or gift store that sells the books on consignment — or, while it is still
/// at the <see cref="StockistStage.Lead"/>…<see cref="StockistStage.Interested"/> stages,
/// one that might. Keeping leads and stores as one record means "they said yes" is just
/// a stage change, and nothing typed in about the lead is lost.
/// </summary>
public class Stockist : AggregateRoot
{
    public const int MaxNameLength = 200;
    public const int MaxRegionLength = 64;
    public const int MaxAddressLength = 300;
    public const int MaxContactNameLength = 100;
    public const int MaxEmailLength = 255;
    public const int MaxPhoneLength = 30;

    public string Name { get; private set; }
    public string? Region { get; private set; }
    public string? Address { get; private set; }
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    public StockistStage Stage { get; private set; }

    /// <summary>Last call, visit, delivery or stock check — what "no contact for 3 months" is measured from.</summary>
    public DateOnly? LastContactedOn { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    private readonly List<StockLevel> _stock = new();
    public IReadOnlyCollection<StockLevel> Stock => _stock.AsReadOnly();

    private readonly List<StockistInvoice> _invoices = new();
    public IReadOnlyCollection<StockistInvoice> Invoices => _invoices.AsReadOnly();

    private readonly List<StockistEvent> _events = new();
    public IReadOnlyCollection<StockistEvent> Events => _events.AsReadOnly();

    public bool IsStore => Stage is StockistStage.Active or StockistStage.Inactive;

    public int TotalBooks => _stock.Sum(s => s.Quantity);

    private Stockist()
    {
        Name = null!;
    }

    public static Stockist Create(StockistDetails details, StockistStage stage, DateOnly today)
    {
        var stockist = new Stockist { Stage = stage };
        stockist.Apply(details);
        stockist._events.Add(StockistEvent.Created(stockist.Id, today));
        stockist.Touch();
        return stockist;
    }

    public void UpdateDetails(StockistDetails details)
    {
        Apply(details);
        Touch();
    }

    public void ChangeStage(StockistStage stage, DateOnly today)
    {
        if (stage == Stage)
            return;

        var previous = Stage;
        Stage = stage;
        _events.Add(StockistEvent.StageChanged(Id, today, previous, stage));
        Touch();
    }

    public int QuantityOf(string title)
    {
        var code = BookTitles.Normalize(title);
        return _stock.FirstOrDefault(s => s.Title == code)?.Quantity ?? 0;
    }

    /// <summary>
    /// Books left with the store. Dropping books off makes a lead (or a store that had
    /// stopped) an active store, and counts as being in touch with them.
    /// </summary>
    public void RecordDelivery(string title, int quantity, DateOnly on, string? note = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1", nameof(quantity));

        var code = BookTitles.Normalize(title);
        SetQuantity(code, QuantityOf(code) + quantity);
        _events.Add(StockistEvent.Delivered(Id, on, code, quantity, CleanText(note)));

        if (Stage != StockistStage.Active)
            ChangeStage(StockistStage.Active, on);
        MarkContacted(on);
    }

    /// <summary>Unsold books taken back from the store.</summary>
    public void RecordReturn(string title, int quantity, DateOnly on, string? note = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1", nameof(quantity));

        var code = BookTitles.Normalize(title);
        var current = QuantityOf(code);
        if (quantity > current)
            throw new ArgumentException($"The store only has {current} of this title", nameof(quantity));

        SetQuantity(code, current - quantity);
        _events.Add(StockistEvent.Returned(Id, on, code, quantity, CleanText(note)));
        MarkContacted(on);
    }

    /// <summary>
    /// "Called, still 5 in stock": sets what the store has now. The difference from the
    /// previous number is what sold since, which the history keeps.
    /// </summary>
    public void RecordStockCount(string title, int quantity, DateOnly on, string? note = null)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));

        var code = BookTitles.Normalize(title);
        var previous = QuantityOf(code);
        SetQuantity(code, quantity);
        _events.Add(StockistEvent.Counted(Id, on, code, previous, quantity, CleanText(note)));
        MarkContacted(on);
    }

    /// <summary>
    /// Stock carried over from an old record (the Excel sheet) with no date of its own.
    /// It is written to the history but, unlike a stock check, is not a contact.
    /// </summary>
    public void SetOpeningStock(string title, int quantity, DateOnly on, string? note = null)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));

        var code = BookTitles.Normalize(title);
        var previous = QuantityOf(code);
        SetQuantity(code, quantity);
        _events.Add(StockistEvent.Counted(Id, on, code, previous, quantity, CleanText(note)));
        Touch();
    }

    /// <summary>A call, email or visit. Resets the "no contact" reminder.</summary>
    public void LogContact(string text, DateOnly on)
    {
        _events.Add(StockistEvent.Contacted(Id, on, RequireText(text)));
        MarkContacted(on);
    }

    /// <summary>A note that is not a contact — it does not reset the reminder.</summary>
    public void AddNote(string text, DateOnly on)
    {
        _events.Add(StockistEvent.Note(Id, on, RequireText(text)));
        Touch();
    }

    public StockistInvoice AddInvoice(DateOnly sentOn, decimal? amount, string? reference)
    {
        var invoice = new StockistInvoice(Id, sentOn, amount, reference);
        _invoices.Add(invoice);
        _events.Add(StockistEvent.InvoiceSent(Id, sentOn, invoice.Amount, invoice.Reference));
        Touch();
        return invoice;
    }

    public void MarkInvoicePaid(Guid invoiceId, DateOnly paidOn)
    {
        var invoice = FindInvoice(invoiceId);
        if (invoice.IsPaid)
            return;

        invoice.MarkPaid(paidOn);
        _events.Add(StockistEvent.InvoicePaid(Id, paidOn, invoice.Amount, invoice.Reference));
        Touch();
    }

    /// <summary>Undo for a payment ticked by mistake.</summary>
    public void MarkInvoiceUnpaid(Guid invoiceId)
    {
        FindInvoice(invoiceId).MarkUnpaid();
        Touch();
    }

    public void RemoveInvoice(Guid invoiceId)
    {
        _invoices.Remove(FindInvoice(invoiceId));
        Touch();
    }

    private StockistInvoice FindInvoice(Guid invoiceId) =>
        _invoices.FirstOrDefault(i => i.Id == invoiceId)
        ?? throw new ArgumentException($"Invoice {invoiceId} does not belong to this store", nameof(invoiceId));

    private void SetQuantity(string code, int quantity)
    {
        var level = _stock.FirstOrDefault(s => s.Title == code);
        if (level == null)
            _stock.Add(new StockLevel(code, quantity));
        else
            level.Set(quantity);
    }

    private void MarkContacted(DateOnly on)
    {
        // Back-dated entries must not move the date backwards
        if (LastContactedOn == null || on > LastContactedOn)
            LastContactedOn = on;
        Touch();
    }

    private void Apply(StockistDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
            throw new ArgumentException("Store name cannot be empty", nameof(details));

        Name = Limit(details.Name.Trim(), MaxNameLength, "Name");
        Region = Limit(NullIfBlank(details.Region), MaxRegionLength, "Region");
        Address = Limit(NullIfBlank(details.Address), MaxAddressLength, "Address");
        ContactName = Limit(NullIfBlank(details.ContactName), MaxContactNameLength, "Contact name");
        Email = Limit(NullIfBlank(details.Email), MaxEmailLength, "Email");
        Phone = Limit(NullIfBlank(details.Phone), MaxPhoneLength, "Phone");
    }

    private void Touch()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = LastActivityAt;
    }

    private static string RequireText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));
        return CleanText(text)!;
    }

    private static string? CleanText(string? text)
    {
        var value = NullIfBlank(text);
        if (value?.Length > StockistEvent.MaxTextLength)
            throw new ArgumentException($"Text must not exceed {StockistEvent.MaxTextLength} characters", nameof(text));
        return value;
    }

    private static string? Limit(string? value, int max, string field)
    {
        if (value?.Length > max)
            throw new ArgumentException($"{field} must not exceed {max} characters");
        return value;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>What a person types about a store; everything but the name is optional.</summary>
public record StockistDetails(
    string Name,
    string? Region = null,
    string? Address = null,
    string? ContactName = null,
    string? Email = null,
    string? Phone = null);

/// <summary>
/// Lead, Contacted and Interested are opportunities; Active stores carry books;
/// Inactive ones stopped (books taken back) but are kept for their history.
/// </summary>
public enum StockistStage
{
    Lead,
    Contacted,
    Interested,
    Active,
    Inactive
}

/// <summary>How many copies of one title a store has right now.</summary>
public class StockLevel
{
    public string Title { get; private set; }
    public int Quantity { get; private set; }

    private StockLevel()
    {
        Title = null!;
    }

    internal StockLevel(string title, int quantity)
    {
        Title = title;
        Quantity = quantity;
    }

    internal void Set(int quantity) => Quantity = quantity;
}

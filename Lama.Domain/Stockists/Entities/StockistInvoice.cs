using Lama.Domain.Common;

namespace Lama.Domain.Stockists.Entities;

/// <summary>
/// An invoice sent to a store. The invoice itself is made elsewhere; the CRM only
/// remembers when it went out and whether it has been paid, so unpaid ones can be chased.
/// </summary>
public class StockistInvoice : Entity
{
    public const int MaxReferenceLength = 100;

    public Guid StockistId { get; private set; }
    public DateOnly SentOn { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Reference { get; private set; }
    public DateOnly? PaidOn { get; private set; }

    public bool IsPaid => PaidOn.HasValue;

    private StockistInvoice() { }

    internal StockistInvoice(Guid stockistId, DateOnly sentOn, decimal? amount, string? reference)
    {
        StockistId = stockistId;
        Apply(sentOn, amount, reference);
    }

    /// <summary>Corrects an invoice typed in wrong. A paid invoice stays paid.</summary>
    internal void Change(DateOnly sentOn, decimal? amount, string? reference)
    {
        if (PaidOn < sentOn)
            throw new ArgumentException("An invoice cannot be paid before it was sent", nameof(sentOn));
        Apply(sentOn, amount, reference);
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(DateOnly sentOn, decimal? amount, string? reference)
    {
        if (amount is < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        var cleaned = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (cleaned?.Length > MaxReferenceLength)
            throw new ArgumentException($"Reference must not exceed {MaxReferenceLength} characters", nameof(reference));

        SentOn = sentOn;
        Amount = amount;
        Reference = cleaned;
    }

    internal void MarkPaid(DateOnly paidOn)
    {
        if (paidOn < SentOn)
            throw new ArgumentException("An invoice cannot be paid before it was sent", nameof(paidOn));
        PaidOn = paidOn;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void MarkUnpaid()
    {
        PaidOn = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

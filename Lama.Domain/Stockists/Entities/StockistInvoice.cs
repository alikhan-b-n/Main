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
        if (amount is < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        StockistId = stockistId;
        SentOn = sentOn;
        Amount = amount;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (Reference?.Length > MaxReferenceLength)
            throw new ArgumentException($"Reference must not exceed {MaxReferenceLength} characters", nameof(reference));
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

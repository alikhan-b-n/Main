using Lama.Domain.Common;

namespace Lama.Domain.Expenses.Entities;

/// <summary>
/// Money the business pays out, set against its sales. Either a one-off cost on a day
/// (shipping books to a store) or a monthly charge from a day on (Shopify, the CRM),
/// which counts once in every month it runs until it is stopped.
/// </summary>
public class Expense : AggregateRoot
{
    public const int MaxNoteLength = 500;
    public const decimal MaxAmount = 1_000_000m;

    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>The day it was paid; for a monthly charge, the first month it applies to.</summary>
    public DateOnly StartsOn { get; private set; }

    public bool Monthly { get; private set; }

    /// <summary>A monthly charge that was cancelled: the last month it applies to. Null while it runs.</summary>
    public DateOnly? EndsOn { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Shipping sent to a store keeps a link to it, so the cost shows against that store.</summary>
    public Guid? StockistId { get; private set; }

    private Expense() { }

    public static Expense Create(
        ExpenseCategory category, decimal amount, DateOnly startsOn, bool monthly, string? note,
        DateOnly? endsOn = null, Guid? stockistId = null)
    {
        var expense = new Expense { StockistId = stockistId };
        expense.Apply(category, amount, startsOn, monthly, endsOn, note);
        return expense;
    }

    public void Update(ExpenseCategory category, decimal amount, DateOnly startsOn, bool monthly, DateOnly? endsOn, string? note)
    {
        Apply(category, amount, startsOn, monthly, endsOn, note);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// What this expense costs in the given month: zero in a month it does not touch.
    /// A monthly charge counts only for months that have begun by <paramref name="today"/>:
    /// December's Shopify bill is not spent yet in November.
    /// </summary>
    public decimal AmountIn(int year, int month, DateOnly today)
    {
        var index = MonthIndex(year, month);
        if (!Monthly)
            return MonthIndex(StartsOn) == index ? Amount : 0;

        var runs = index >= MonthIndex(StartsOn)
                   && index <= MonthIndex(today)
                   && (EndsOn is null || index <= MonthIndex(EndsOn.Value));
        return runs ? Amount : 0;
    }

    /// <summary>The total over a year so far: up to twelve charges for a monthly one.</summary>
    public decimal AmountIn(int year, DateOnly today) => Enumerable.Range(1, 12).Sum(m => AmountIn(year, m, today));

    /// <summary>Whether it costs anything in the year so far, so the year's list can show it.</summary>
    public bool Touches(int year, DateOnly today) => AmountIn(year, today) > 0;

    private void Apply(ExpenseCategory category, decimal amount, DateOnly startsOn, bool monthly, DateOnly? endsOn, string? note)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentException($"Unknown category: {category}", nameof(category));
        if (amount <= 0 || amount > MaxAmount)
            throw new ArgumentException("Amount must be more than zero", nameof(amount));
        if (monthly && endsOn is { } end && MonthIndex(end) < MonthIndex(startsOn))
            throw new ArgumentException("A monthly expense cannot end before it starts", nameof(endsOn));

        var cleaned = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (cleaned?.Length > MaxNoteLength)
            throw new ArgumentException($"Note cannot be longer than {MaxNoteLength} characters", nameof(note));

        Category = category;
        Amount = decimal.Round(amount, 2);
        StartsOn = startsOn;
        Monthly = monthly;
        EndsOn = monthly ? endsOn : null;
        Note = cleaned;
    }

    private static int MonthIndex(DateOnly day) => MonthIndex(day.Year, day.Month);
    private static int MonthIndex(int year, int month) => year * 12 + month - 1;
}

public enum ExpenseCategory
{
    /// <summary>Postage or courier for books sent to a store.</summary>
    Shipping,
    Shopify,
    /// <summary>Hosting and upkeep of this CRM.</summary>
    Crm,
    Other
}

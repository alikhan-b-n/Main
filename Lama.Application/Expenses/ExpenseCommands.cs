using FluentValidation;
using Lama.Application.Common;
using Lama.Application.Stockists;
using Lama.Domain.Expenses.Entities;

namespace Lama.Application.Expenses;

/// <summary>Expenses, read-only for the totals or tracked one at a time for changes.</summary>
public interface IExpenseRepository
{
    /// <summary>Every expense, read-only — a few a week at most.</summary>
    Task<IReadOnlyList<Expense>> ListAsync(CancellationToken cancellationToken = default);

    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Queues a new expense; it is written by the next save of this unit of work.</summary>
    void Add(Expense expense);

    void Remove(Expense expense);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class ExpenseNotFoundException : Exception
{
    public ExpenseNotFoundException(Guid id) : base($"Expense {id} was not found") { }
}

/// <summary>
/// One expense as the list shows it. <see cref="InYear"/> is what it costs in the year
/// asked for: the amount for a one-off, the amount times the months it ran for a monthly one.
/// </summary>
public record ExpenseDto(
    Guid Id,
    // "Shipping", "Shopify", "Crm" or "Other"
    string Category,
    decimal Amount,
    DateOnly StartsOn,
    bool Monthly,
    DateOnly? EndsOn,
    string? Note,
    Guid? StockistId,
    string? StockistName,
    decimal InYear);

/// <summary>The expenses that cost something in a year (default: this year), newest first.</summary>
public record GetExpensesQuery(int? Year = null) : IQuery<IReadOnlyList<ExpenseDto>>;

public record SaveExpenseCommand(
    Guid? Id,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly StartsOn,
    bool Monthly,
    DateOnly? EndsOn,
    string? Note) : ICommand<Guid>;

public record DeleteExpenseCommand(Guid Id) : ICommand;

public class ExpenseHandlers :
    IQueryHandler<GetExpensesQuery, IReadOnlyList<ExpenseDto>>,
    ICommandHandler<SaveExpenseCommand, Guid>,
    ICommandHandler<DeleteExpenseCommand>
{
    private readonly IExpenseRepository _expenses;
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public ExpenseHandlers(IExpenseRepository expenses, IStockistRepository stockists, TimeProvider clock)
    {
        _expenses = expenses;
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ExpenseDto>> Handle(GetExpensesQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var year = query.Year ?? today.Year;
        var expenses = await _expenses.ListAsync(cancellationToken);
        var stores = (await _stockists.ListAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

        return expenses
            .Where(e => e.Touches(year, today))
            .OrderByDescending(e => e.Monthly)
            .ThenByDescending(e => e.StartsOn)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseDto(e.Id, e.Category.ToString(), e.Amount, e.StartsOn, e.Monthly, e.EndsOn, e.Note,
                e.StockistId, e.StockistId is { } id ? stores.GetValueOrDefault(id) : null, e.AmountIn(year, today)))
            .ToList();
    }

    public async Task<Guid> Handle(SaveExpenseCommand command, CancellationToken cancellationToken)
    {
        if (command.Id is not { } id)
        {
            var expense = Expense.Create(command.Category, command.Amount, command.StartsOn, command.Monthly,
                command.Note, command.EndsOn);
            _expenses.Add(expense);
            await _expenses.SaveChangesAsync(cancellationToken);
            return expense.Id;
        }

        var existing = await _expenses.GetByIdAsync(id, cancellationToken) ?? throw new ExpenseNotFoundException(id);
        existing.Update(command.Category, command.Amount, command.StartsOn, command.Monthly, command.EndsOn, command.Note);
        await _expenses.SaveChangesAsync(cancellationToken);
        return existing.Id;
    }

    public async Task Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetByIdAsync(command.Id, cancellationToken)
                      ?? throw new ExpenseNotFoundException(command.Id);
        _expenses.Remove(expense);
        await _expenses.SaveChangesAsync(cancellationToken);
    }
}

public class SaveExpenseCommandValidator : AbstractValidator<SaveExpenseCommand>
{
    public SaveExpenseCommandValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be more than zero")
            .LessThanOrEqualTo(Expense.MaxAmount);
        RuleFor(x => x.EndsOn)
            .Must((x, end) => end is null || new DateOnly(end.Value.Year, end.Value.Month, 1) >= new DateOnly(x.StartsOn.Year, x.StartsOn.Month, 1))
            .When(x => x.Monthly)
            .WithMessage("A monthly expense cannot end before it starts");
        RuleFor(x => x.Note).MaximumLength(Expense.MaxNoteLength);
    }
}

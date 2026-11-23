using Lama.Application.Expenses;
using Lama.Application.Stockists;
using Lama.Application.Stockists.Commands;
using Lama.Application.Stockists.Queries;
using Lama.Application.Stockists.Validators;
using Lama.Domain.Expenses.Entities;
using Lama.Domain.Stockists.Entities;
using Microsoft.Extensions.Time.Testing;

namespace Lama.Tests.Expenses;

public class ExpenseTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateOnly Later = new(2030, 1, 1);

    [Fact]
    public void AOneOffCostsOnlyInItsOwnMonth()
    {
        var shipping = Expense.Create(ExpenseCategory.Shipping, 8.50m, new DateOnly(2026, 11, 5), monthly: false, note: null);

        Assert.Equal(8.50m, shipping.AmountIn(2026, 11, Today));
        Assert.Equal(0m, shipping.AmountIn(2026, 12, Today));
        Assert.Equal(8.50m, shipping.AmountIn(2026, Today));
        Assert.False(shipping.Touches(2027, Later));
    }

    [Fact]
    public void AMonthlyChargeCountsEveryMonthFromItsStartUntilItEnds()
    {
        var crm = Expense.Create(ExpenseCategory.Crm, 20m, new DateOnly(2026, 11, 1), monthly: true, note: "Hosting");

        Assert.Equal(0m, crm.AmountIn(2026, 10, Later));
        Assert.Equal(40m, crm.AmountIn(2026, Later));
        Assert.Equal(240m, crm.AmountIn(2027, Later));

        crm.Update(ExpenseCategory.Crm, 20m, crm.StartsOn, monthly: true, endsOn: new DateOnly(2027, 3, 1), note: "Hosting");

        Assert.Equal(60m, crm.AmountIn(2027, Later));
        Assert.Equal(0m, crm.AmountIn(2027, 4, Later));
    }

    [Fact]
    public void AMonthlyChargeIsNotCountedForMonthsThatHaveNotBegun()
    {
        var shopify = Expense.Create(ExpenseCategory.Shopify, 30m, new DateOnly(2026, 1, 1), monthly: true, note: null);

        // On 23 November: January to November, not December
        Assert.Equal(330m, shopify.AmountIn(2026, Today));
        Assert.Equal(0m, shopify.AmountIn(2026, 12, Today));
    }

    [Fact]
    public void AMonthlyChargeCannotEndBeforeItStarts() =>
        Assert.Throws<ArgumentException>(() =>
            Expense.Create(ExpenseCategory.Shopify, 39m, new DateOnly(2026, 5, 1), monthly: true, note: null,
                endsOn: new DateOnly(2026, 4, 30)));

    [Fact]
    public void FinancialsSetTheExpensesAgainstTheSales()
    {
        var store = Stockist.Create(new StockistDetails("Kennys, Galway"), StockistStage.Active, Today);
        store.AddInvoice(new DateOnly(2026, 11, 2), 100m, null);
        var expenses = new[]
        {
            Expense.Create(ExpenseCategory.Shipping, 8.50m, new DateOnly(2026, 11, 2), false, null, stockistId: store.Id),
            Expense.Create(ExpenseCategory.Crm, 20m, new DateOnly(2026, 11, 1), true, null),
            Expense.Create(ExpenseCategory.Shopify, 30m, new DateOnly(2025, 12, 1), true, null)
        };

        var result = StockistFinancialsCalculator.Calculate(new[] { store }, 2026, Today, null, expenses);

        // On 23 November: Shopify January to November, the CRM November only
        Assert.Equal(new ExpenseTotals(358.50m, 8.50m, 330m, 20m, 0m, 100m - 358.50m), result.Expenses);
        Assert.Equal(58.50m, result.ByMonth[10].Expenses);
        Assert.Equal(0m, result.ByMonth[11].Expenses);
        Assert.Contains(2025, result.Years);
        Assert.Equal(30m, result.ByYear.Single(y => y.Year == 2025).Expenses);
    }

    [Fact]
    public async Task ShippingOnADeliveryIsWrittenToTheStoreAndKeptAsAnExpense()
    {
        var store = Stockist.Create(new StockistDetails("Kennys, Galway"), StockistStage.Active, Today);
        var stockists = new OneStockist(store);
        var expenses = new InMemoryExpenses();
        var handlers = new StockistCommandHandlers(stockists, expenses, new FakeTimeProvider(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero)));

        await handlers.Handle(new RecordStockCommand(store.Id, StockMovement.Restock, "cork", 2, Added: 10, ShippingCost: 7.95m),
            CancellationToken.None);

        var expense = Assert.Single(expenses.Items);
        Assert.Equal(ExpenseCategory.Shipping, expense.Category);
        Assert.Equal(7.95m, expense.Amount);
        Assert.Equal(store.Id, expense.StockistId);
        Assert.Equal(Today, expense.StartsOn);
        Assert.Contains(store.Events, e => e.Kind == StockistEventKind.Shipping && e.Amount == 7.95m);
        Assert.Equal(12, store.QuantityOf("cork"));
    }

    [Fact]
    public void ShippingIsRefusedWhenNoBooksAreSent()
    {
        var validator = new RecordStockCommandValidator();

        Assert.False(validator.Validate(new RecordStockCommand(Guid.NewGuid(), StockMovement.Count, "cork", 3, ShippingCost: 5m)).IsValid);
        Assert.False(validator.Validate(new RecordStockCommand(Guid.NewGuid(), StockMovement.Restock, "cork", 3, ShippingCost: 5m)).IsValid);
        Assert.True(validator.Validate(new RecordStockCommand(Guid.NewGuid(), StockMovement.Delivery, "cork", 3, ShippingCost: 5m)).IsValid);
    }

    private sealed class OneStockist : IStockistRepository
    {
        private readonly Stockist _stockist;
        public OneStockist(Stockist stockist) => _stockist = stockist;

        public Task<IReadOnlyList<Stockist>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Stockist>>(new[] { _stockist });
        public Task<Stockist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == _stockist.Id ? _stockist : null);
        public Task<IReadOnlySet<string>> GetNamesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        public Task AddAsync(Stockist stockist, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<Stockist> stockists, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Stockist stockist, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryExpenses : IExpenseRepository
    {
        public List<Expense> Items { get; } = new();

        public Task<IReadOnlyList<Expense>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Expense>>(Items);
        public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(e => e.Id == id));
        public void Add(Expense expense) => Items.Add(expense);
        public void Remove(Expense expense) => Items.Remove(expense);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

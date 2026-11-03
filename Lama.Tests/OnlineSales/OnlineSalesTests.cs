using Lama.Application.OnlineSales;
using Lama.Application.Stockists.Queries;
using Lama.Domain.OnlineSales.Entities;
using Lama.Domain.Stockists.Entities;
using Lama.Infrastructure.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Lama.Tests.OnlineSales;

public class OnlineOrderTests
{
    [Theory]
    [InlineData("Noah's Cork Adventure", "cork")]
    [InlineData("NOAH'S BERLIN ADVENTURE", "berlin")]
    [InlineData("Cork & Kerry Gift Bundle", null)]
    [InlineData("Gift wrapping", null)]
    [InlineData("Corkscrew", null)]
    public void FindsTheBookByThePlaceInItsName(string product, string? expected) =>
        Assert.Equal(expected, OnlineOrder.BookTitleOf(product));

    [Fact]
    public void AnUpdateReplacesTheLinesAndDropsRefundedOnes()
    {
        var order = OnlineOrder.Create("gid://shopify/Order/1", Snapshot(
            amount: 23.90m,
            new OnlineOrderLineSnapshot("Noah's Cork Adventure", 1),
            new OnlineOrderLineSnapshot("Noah's Kerry Adventure", 1)));

        order.Update(Snapshot(amount: 11.95m,
            new OnlineOrderLineSnapshot("Noah's Cork Adventure", 1),
            new OnlineOrderLineSnapshot("Noah's Kerry Adventure", 0)));

        Assert.Equal(11.95m, order.Amount);
        var line = Assert.Single(order.Lines);
        Assert.Equal("cork", line.Title);
    }

    internal static OnlineOrderSnapshot Snapshot(decimal amount, params OnlineOrderLineSnapshot[] lines) =>
        new("#1001", new DateOnly(2026, 3, 4), amount, "EUR", false, new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc), lines);
}

public class ShopifyRequestTests
{
    // The example from Shopify's OAuth documentation
    [Fact]
    public void AcceptsShopifysSignatureAndRejectsATamperedOne()
    {
        var query = new Dictionary<string, string>
        {
            ["code"] = "0907a61c0c8d55e99db179b68161bc00",
            ["hmac"] = "700e2dadb827fcc8609e9d5ce208b2e9cdaab9df07390d2cbca10d7c328fc4bf",
            ["shop"] = "some-shop.myshopify.com",
            ["state"] = "0.6784241404160823",
            ["timestamp"] = "1337178173"
        };

        Assert.True(ShopifyRequests.HasValidHmac(query, "hush"));

        query["shop"] = "other-shop.myshopify.com";
        Assert.False(ShopifyRequests.HasValidHmac(query, "hush"));
    }

    [Theory]
    [InlineData("04dff3-2.myshopify.com", true)]
    [InlineData("evil.com", false)]
    [InlineData("shop.myshopify.com.evil.com", false)]
    public void OnlyAcceptsMyshopifyDomains(string shop, bool valid) =>
        Assert.Equal(valid, ShopifyRequests.IsShopDomain(shop));
}

public class ShopifyOrderSyncTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryOnlineSales _repository = new();
    private readonly FakeShopify _shopify = new();

    private ShopifyOrderSync Sync() => new(_repository, _shopify, new FakeTimeProvider(Now),
        NullLogger<ShopifyOrderSync>.Instance);

    [Fact]
    public async Task DoesNothingBeforeTheAppIsInstalled()
    {
        var result = await Sync().SyncAsync();

        Assert.Equal(ShopifySyncOutcome.NotConnected, result.Outcome);
    }

    [Fact]
    public async Task CopiesTheHistoryPageByPageThenOnlyWhatChanged()
    {
        _repository.Connection = Connected();
        _shopify.Pages.Add(new ShopifyOrderPage(new[] { Order("1", 11.95m, At(1)), Order("2", 23.90m, At(2)) }, true, "c1"));
        _shopify.Pages.Add(new ShopifyOrderPage(new[] { Order("3", 11.95m, At(3), test: true) }, false, null));

        var first = await Sync().SyncAsync();

        Assert.Equal(new ShopifySyncResult(ShopifySyncOutcome.Synced, 2), first);
        Assert.Equal(new[] { "1", "2" }, _repository.Orders.Keys.Order());
        Assert.Null(_shopify.Requests[0].Since);
        Assert.Equal("c1", _shopify.Requests[1].After);
        Assert.Equal(At(3), _repository.Connection.SyncedUpTo);

        // Order 1 is refunded later; the next run asks only for changes, a little early
        _shopify.Pages.Add(new ShopifyOrderPage(new[] { Order("1", 0m, At(5)) }, false, null));
        await Sync().SyncAsync();

        Assert.Equal(At(3).AddMinutes(-5), _shopify.Requests[2].Since);
        Assert.Equal(0m, _repository.Orders["1"].Amount);
        Assert.Equal(2, _repository.Orders.Count);
    }

    [Fact]
    public async Task RemembersWhyASyncFailed()
    {
        _repository.Connection = Connected();
        _shopify.Failure = new ShopifyUnavailableException("Shopify answered 401");

        var result = await Sync().SyncAsync();

        Assert.Equal(ShopifySyncOutcome.Failed, result.Outcome);
        Assert.Equal("Shopify answered 401", _repository.Connection.LastSyncError);
        Assert.Null(_repository.Connection.SyncedUpTo);
    }

    [Fact]
    public void PlacesAnOrderOnTheStoresDay()
    {
        var lateEvening = Order("1", 10m, new DateTime(2026, 6, 30, 23, 30, 0, DateTimeKind.Utc));

        var snapshot = ShopifyOrderSync.ToSnapshot(lateEvening, ShopifyOrderSync.FindTimeZone("Europe/Dublin"));

        // 00:30 on 1 July in Irish summer time
        Assert.Equal(new DateOnly(2026, 7, 1), snapshot.PlacedOn);
    }

    private static ShopifyConnection Connected() =>
        ShopifyConnection.Create("04dff3-2.myshopify.com", new ShopifyToken("shpat_test", "read_orders"), Now);

    private static DateTime At(int day) => new(2026, 10, day, 9, 0, 0, DateTimeKind.Utc);

    private static ShopifyOrder Order(string id, decimal amount, DateTime changed, bool test = false) =>
        new(id, "#" + id, changed, changed, false, test, amount, "EUR",
            new[] { new OnlineOrderLineSnapshot("Noah's Cork Adventure", 1) });

    private sealed class FakeShopify : IShopifyClient
    {
        public List<ShopifyOrderPage> Pages { get; } = new();
        public List<(DateTime? Since, string? After)> Requests { get; } = new();
        public Exception? Failure { get; set; }

        public bool IsConfigured => true;
        public string Shop => "04dff3-2.myshopify.com";

        public Task<ShopifyToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ShopifyToken("shpat_new", "read_orders"));

        public Task<ShopifyToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ShopifyToken("shpat_renewed", null));

        public Task<string> GetTimeZoneAsync(string accessToken, CancellationToken cancellationToken = default) =>
            Failure != null ? Task.FromException<string>(Failure) : Task.FromResult("Europe/Dublin");

        public Task<ShopifyOrderPage> GetOrdersAsync(
            string accessToken, DateTime? changedSince, string? after, CancellationToken cancellationToken = default)
        {
            Requests.Add((changedSince, after));
            var page = Pages[0];
            Pages.RemoveAt(0);
            return Task.FromResult(page);
        }
    }

    private sealed class InMemoryOnlineSales : IOnlineSalesRepository
    {
        public ShopifyConnection? Connection { get; set; }
        public Dictionary<string, OnlineOrder> Orders { get; } = new();

        public Task<ShopifyConnection?> GetConnectionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Connection);

        public Task AddConnectionAsync(ShopifyConnection connection, CancellationToken cancellationToken = default)
        {
            Connection = connection;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, OnlineOrder>> GetOrdersAsync(
            IEnumerable<string> shopifyIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, OnlineOrder> found = shopifyIds
                .Where(Orders.ContainsKey)
                .Distinct()
                .ToDictionary(id => id, id => Orders[id]);
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<OnlineOrder>> ListOrdersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OnlineOrder>>(Orders.Values.ToList());

        public Task<int> CountOrdersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Orders.Values.Count(o => o.Counts));

        public void AddOrder(OnlineOrder order) => Orders[order.ShopifyId] = order;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public class OnlineFinancialsTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    [Fact]
    public void PutsWebsiteSalesNextToTheInvoicesWithoutCancelledOrders()
    {
        var store = Stockist.Create(new StockistDetails("Kennys, Galway"), StockistStage.Active, Today);
        store.AddInvoice(new DateOnly(2026, 3, 1), 100m, null);
        var orders = new[]
        {
            Placed("1", new DateOnly(2026, 3, 10), 11.95m),
            Placed("2", new DateOnly(2026, 3, 20), 23.90m),
            Placed("3", new DateOnly(2026, 3, 21), 11.95m, cancelled: true),
            Placed("4", new DateOnly(2024, 12, 1), 11.95m)
        };

        var result = StockistFinancialsCalculator.Calculate(new[] { store }, 2026, Today, orders);

        Assert.Equal(new OnlineTotals(35.85m, 2), result.Online);
        Assert.Equal(new MonthTotals(3, 100m, 0m, 35.85m), result.ByMonth[2]);
        // Online sales are paid at checkout: nothing more is outstanding
        Assert.Equal(100m, result.Totals.Outstanding);
        Assert.Equal(new[] { 2026, 2024 }, result.Years);
        var earlier = result.ByYear.Single(y => y.Year == 2024);
        Assert.Equal((0m, 11.95m, 1), (earlier.Invoiced, earlier.Online, earlier.OnlineOrderCount));
    }

    private static OnlineOrder Placed(string id, DateOnly on, decimal amount, bool cancelled = false) =>
        OnlineOrder.Create(id, new OnlineOrderSnapshot("#" + id, on, amount, "EUR", cancelled,
            on.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Array.Empty<OnlineOrderLineSnapshot>()));
}

using Lama.Domain.OnlineSales.Entities;
using Microsoft.Extensions.Logging;

namespace Lama.Application.OnlineSales;

public enum ShopifySyncOutcome
{
    Synced,
    NotConnected,
    Failed
}

public record ShopifySyncResult(ShopifySyncOutcome Outcome, int OrdersRead = 0, string? Error = null);

/// <summary>
/// Copies orders from Shopify: the whole history on the first run, then only orders
/// created or changed since the last one. Writing an order twice is harmless, so each run
/// starts a few minutes early rather than risk missing an order changed mid-run.
/// </summary>
public class ShopifyOrderSync
{
    private static readonly TimeSpan Overlap = TimeSpan.FromMinutes(5);

    private readonly IOnlineSalesRepository _repository;
    private readonly IShopifyClient _shopify;
    private readonly TimeProvider _clock;
    private readonly ILogger<ShopifyOrderSync> _logger;

    public ShopifyOrderSync(
        IOnlineSalesRepository repository, IShopifyClient shopify, TimeProvider clock, ILogger<ShopifyOrderSync> logger)
    {
        _repository = repository;
        _shopify = shopify;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ShopifySyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _repository.GetConnectionAsync(cancellationToken);
        if (connection == null || !_shopify.IsConfigured)
            return new ShopifySyncResult(ShopifySyncOutcome.NotConnected);

        try
        {
            var read = await CopyOrdersAsync(connection, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Shopify sync read {Count} orders", read);
            return new ShopifySyncResult(ShopifySyncOutcome.Synced, read);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Shopify sync failed");
            // Pages saved before the failure stay; the cursor did not move, so the next run redoes the rest
            connection.RecordSyncFailure(Now, ex.Message);
            await _repository.SaveChangesAsync(cancellationToken);
            return new ShopifySyncResult(ShopifySyncOutcome.Failed, Error: ex.Message);
        }
    }

    private async Task<int> CopyOrdersAsync(ShopifyConnection connection, CancellationToken cancellationToken)
    {
        if (connection.TokenNeedsRefresh(Now))
        {
            connection.UseToken(await _shopify.RefreshAsync(connection.RefreshToken!, cancellationToken));
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var timeZone = FindTimeZone(await _shopify.GetTimeZoneAsync(connection.AccessToken, cancellationToken));
        var since = connection.SyncedUpTo - Overlap;
        DateTime? newest = null;
        string? after = null;
        var read = 0;

        while (true)
        {
            var page = await _shopify.GetOrdersAsync(connection.AccessToken, since, after, cancellationToken);
            var existing = await _repository.GetOrdersAsync(page.Orders.Select(o => o.Id), cancellationToken);

            foreach (var order in page.Orders)
            {
                newest = newest == null || order.UpdatedAtUtc > newest ? order.UpdatedAtUtc : newest;
                // Test orders are Shopify's own checkout trials, not sales
                if (order.Test)
                    continue;

                var snapshot = ToSnapshot(order, timeZone);
                if (existing.TryGetValue(order.Id, out var known))
                    known.Update(snapshot);
                else
                    _repository.AddOrder(OnlineOrder.Create(order.Id, snapshot));
                read++;
            }

            // Saved page by page, so a failure halfway keeps what was already read
            await _repository.SaveChangesAsync(cancellationToken);

            if (!page.HasNextPage || page.EndCursor == null)
                break;
            after = page.EndCursor;
        }

        connection.RecordSync(Now, newest);
        return read;
    }

    public static OnlineOrderSnapshot ToSnapshot(ShopifyOrder order, TimeZoneInfo timeZone) => new(
        order.Name,
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(order.CreatedAtUtc, DateTimeKind.Utc), timeZone)),
        order.Subtotal,
        order.Currency,
        order.Cancelled,
        order.UpdatedAtUtc,
        order.Lines);

    /// <summary>The store's zone, or UTC when this machine does not know it (an hour off at most in Ireland).</summary>
    public static TimeZoneInfo FindTimeZone(string? ianaId)
    {
        if (!string.IsNullOrWhiteSpace(ianaId) && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out var zone))
            return zone;
        return TimeZoneInfo.Utc;
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;
}

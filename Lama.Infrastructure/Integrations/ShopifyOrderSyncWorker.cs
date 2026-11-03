using System.Threading.Channels;
using Lama.Application.OnlineSales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Infrastructure.Integrations;

/// <summary>Wakes the sync early, e.g. right after the app is installed.</summary>
public class ShopifySyncSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Request() => _channel.Writer.TryWrite(true);

    /// <summary>Returns when a sync was requested or the interval passed, whichever is first.</summary>
    public async Task WaitAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(interval);
        try
        {
            await _channel.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The interval passed
        }
    }
}

/// <summary>Copies new and changed Shopify orders every hour, and once shortly after startup.</summary>
public class ShopifyOrderSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ShopifySyncSignal _signal;
    private readonly ShopifyOptions _options;
    private readonly ILogger<ShopifyOrderSyncWorker> _logger;

    public ShopifyOrderSyncWorker(
        IServiceScopeFactory scopes, ShopifySyncSignal signal, IOptions<ShopifyOptions> options,
        ILogger<ShopifyOrderSyncWorker> logger)
    {
        _scopes = scopes;
        _signal = signal;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogInformation("Shopify is not configured; online sales will not be synced");
            return;
        }

        // Let migrations and startup finish first
        await _signal.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.SyncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ShopifyOrderSync>().SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The sync records its own failures; this is for the database being away
                _logger.LogError(ex, "Shopify sync could not run");
            }

            await _signal.WaitAsync(interval, stoppingToken);
        }
    }
}

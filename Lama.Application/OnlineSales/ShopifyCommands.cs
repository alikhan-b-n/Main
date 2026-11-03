using Lama.Application.Common;
using Lama.Domain.OnlineSales.Entities;

namespace Lama.Application.OnlineSales;

/// <summary>The end of an install: the code from Shopify becomes the store's access token.</summary>
public record ConnectShopifyCommand(string Code) : ICommand;

public class ConnectShopifyCommandHandler : ICommandHandler<ConnectShopifyCommand>
{
    private readonly IOnlineSalesRepository _repository;
    private readonly IShopifyClient _shopify;
    private readonly TimeProvider _clock;

    public ConnectShopifyCommandHandler(IOnlineSalesRepository repository, IShopifyClient shopify, TimeProvider clock)
    {
        _repository = repository;
        _shopify = shopify;
        _clock = clock;
    }

    public async Task Handle(ConnectShopifyCommand command, CancellationToken cancellationToken)
    {
        var token = await _shopify.ExchangeCodeAsync(command.Code, cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;

        var connection = await _repository.GetConnectionAsync(cancellationToken);
        if (connection == null)
        {
            await _repository.AddConnectionAsync(ShopifyConnection.Create(_shopify.Shop, token, now), cancellationToken);
            return;
        }

        connection.Reconnect(token, now);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Reads new and changed orders now instead of waiting for the hourly run.</summary>
public record SyncShopifyOrdersCommand : ICommand<ShopifySyncResult>;

public class SyncShopifyOrdersCommandHandler : ICommandHandler<SyncShopifyOrdersCommand, ShopifySyncResult>
{
    private readonly ShopifyOrderSync _sync;

    public SyncShopifyOrdersCommandHandler(ShopifyOrderSync sync)
    {
        _sync = sync;
    }

    public Task<ShopifySyncResult> Handle(SyncShopifyOrdersCommand command, CancellationToken cancellationToken) =>
        _sync.SyncAsync(cancellationToken);
}

public record ShopifyStatusDto(
    bool Configured,
    bool Connected,
    string? Shop,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError,
    int OrderCount
);

public record GetShopifyStatusQuery : IQuery<ShopifyStatusDto>;

public class GetShopifyStatusQueryHandler : IQueryHandler<GetShopifyStatusQuery, ShopifyStatusDto>
{
    private readonly IOnlineSalesRepository _repository;
    private readonly IShopifyClient _shopify;

    public GetShopifyStatusQueryHandler(IOnlineSalesRepository repository, IShopifyClient shopify)
    {
        _repository = repository;
        _shopify = shopify;
    }

    public async Task<ShopifyStatusDto> Handle(GetShopifyStatusQuery query, CancellationToken cancellationToken)
    {
        var connection = await _repository.GetConnectionAsync(cancellationToken);
        return new ShopifyStatusDto(
            _shopify.IsConfigured,
            connection != null,
            connection?.Shop ?? (_shopify.IsConfigured ? _shopify.Shop : null),
            connection?.ConnectedAt,
            connection?.LastSyncedAt,
            connection?.LastSyncError,
            await _repository.CountOrdersAsync(cancellationToken));
    }
}

using Lama.Domain.OnlineSales.Entities;

namespace Lama.Application.OnlineSales;

/// <summary>The Shopify connection and the orders copied from it.</summary>
public interface IOnlineSalesRepository
{
    /// <summary>The connection, tracked for updates, or null before the app is installed.</summary>
    Task<ShopifyConnection?> GetConnectionAsync(CancellationToken cancellationToken = default);

    Task AddConnectionAsync(ShopifyConnection connection, CancellationToken cancellationToken = default);

    /// <summary>The orders already copied among these Shopify ids, tracked, by id.</summary>
    Task<IReadOnlyDictionary<string, OnlineOrder>> GetOrdersAsync(
        IEnumerable<string> shopifyIds, CancellationToken cancellationToken = default);

    /// <summary>Every order, read-only — a small shop has a few thousand at most.</summary>
    Task<IReadOnlyList<OnlineOrder>> ListOrdersAsync(CancellationToken cancellationToken = default);

    Task<int> CountOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>Queues a new order; it is written by the next <see cref="SaveChangesAsync"/>.</summary>
    void AddOrder(OnlineOrder order);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

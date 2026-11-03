using Lama.Domain.OnlineSales.Entities;

namespace Lama.Application.OnlineSales;

/// <summary>
/// The Shopify app the CRM is installed as: the OAuth exchange that grants a token,
/// and the Admin API calls the sync makes with it. One store, set in configuration.
/// </summary>
public interface IShopifyClient
{
    /// <summary>False until the app's client id, secret and the store are configured.</summary>
    bool IsConfigured { get; }

    /// <summary>The store the app may be installed on (its myshopify.com domain).</summary>
    string Shop { get; }

    /// <summary>Trades the code from the install callback for an access token.</summary>
    Task<ShopifyToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Renews an expiring access token.</summary>
    Task<ShopifyToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>The store's time zone (IANA, e.g. "Europe/Dublin"), which decides the day an order belongs to.</summary>
    Task<string> GetTimeZoneAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of orders, oldest change first. With <paramref name="changedSince"/> only orders
    /// created or changed from then on; <paramref name="after"/> is the previous page's cursor.
    /// </summary>
    Task<ShopifyOrderPage> GetOrdersAsync(
        string accessToken, DateTime? changedSince, string? after, CancellationToken cancellationToken = default);
}

public record ShopifyOrderPage(IReadOnlyList<ShopifyOrder> Orders, bool HasNextPage, string? EndCursor);

/// <summary>An order as the Admin API returns it, reduced to what the totals need.</summary>
public record ShopifyOrder(
    string Id,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool Cancelled,
    bool Test,
    decimal Subtotal,
    string Currency,
    IReadOnlyList<OnlineOrderLineSnapshot> Lines
);

/// <summary>Shopify refused or could not be reached; the message says which.</summary>
public class ShopifyUnavailableException : Exception
{
    public ShopifyUnavailableException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

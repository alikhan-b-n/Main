using Lama.Domain.Common;

namespace Lama.Domain.OnlineSales.Entities;

/// <summary>
/// The CRM's access to the bookseller's Shopify store, granted once when the app is
/// installed. There is at most one; installing again replaces the token. The sync
/// remembers how far it got, so each run only asks Shopify for orders changed since.
/// </summary>
public class ShopifyConnection : AggregateRoot
{
    public const int MaxShopLength = 255;
    public const int MaxErrorLength = 1000;

    /// <summary>The store's myshopify.com domain, e.g. "04dff3-2.myshopify.com".</summary>
    public string Shop { get; private set; } = string.Empty;

    public string AccessToken { get; private set; } = string.Empty;
    public string Scopes { get; private set; } = string.Empty;

    /// <summary>Set only when Shopify issued an expiring token; it is then renewed with the refresh token.</summary>
    public DateTime? AccessTokenExpiresAt { get; private set; }

    public string? RefreshToken { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    public DateTime? LastSyncedAt { get; private set; }

    /// <summary>The newest "updated at" among the orders read so far; the next sync starts here.</summary>
    public DateTime? SyncedUpTo { get; private set; }

    /// <summary>Why the last sync failed, cleared by the next one that succeeds.</summary>
    public string? LastSyncError { get; private set; }

    private ShopifyConnection() { }

    public static ShopifyConnection Create(string shop, ShopifyToken token, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(shop))
            throw new ArgumentException("Shop is required", nameof(shop));

        var connection = new ShopifyConnection { Shop = shop.Trim().ToLowerInvariant() };
        connection.Reconnect(token, nowUtc);
        return connection;
    }

    /// <summary>
    /// A new install. Orders already read stay, and the sync carries on from where it was:
    /// a reinstall does not change the store's history.
    /// </summary>
    public void Reconnect(ShopifyToken token, DateTime nowUtc)
    {
        UseToken(token);
        ConnectedAt = nowUtc;
        LastSyncError = null;
        UpdatedAt = nowUtc;
    }

    public void UseToken(ShopifyToken token)
    {
        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new ArgumentException("Access token is required", nameof(token));

        AccessToken = token.AccessToken;
        Scopes = token.Scopes ?? Scopes;
        AccessTokenExpiresAt = token.ExpiresAt;
        RefreshToken = token.RefreshToken ?? RefreshToken;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>An expiring token is renewed a few minutes early, so a slow sync never runs out.</summary>
    public bool TokenNeedsRefresh(DateTime nowUtc) =>
        AccessTokenExpiresAt.HasValue && RefreshToken != null && AccessTokenExpiresAt.Value <= nowUtc.AddMinutes(5);

    public void RecordSync(DateTime nowUtc, DateTime? newestOrderUpdate)
    {
        LastSyncedAt = nowUtc;
        if (newestOrderUpdate.HasValue && (SyncedUpTo == null || newestOrderUpdate > SyncedUpTo))
            SyncedUpTo = newestOrderUpdate;
        LastSyncError = null;
        UpdatedAt = nowUtc;
    }

    public void RecordSyncFailure(DateTime nowUtc, string error)
    {
        LastSyncError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        UpdatedAt = nowUtc;
    }
}

/// <summary>What Shopify hands back for an install or a refresh.</summary>
public record ShopifyToken(string AccessToken, string? Scopes, DateTime? ExpiresAt = null, string? RefreshToken = null);

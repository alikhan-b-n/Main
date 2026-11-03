using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lama.Application.OnlineSales;
using Lama.Domain.OnlineSales.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Infrastructure.Integrations;

/// <summary>
/// The "Noah's Adventures CRM" app in the Shopify Dev Dashboard (custom distribution, one store).
/// Client id and secret come from the app's settings page; the access token is not configured
/// here but granted by installing the app, and kept in the database.
/// </summary>
public class ShopifyOptions
{
    public const string SectionName = "Integrations:Shopify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The store's myshopify.com domain, e.g. "04dff3-2.myshopify.com".</summary>
    public string Shop { get; set; } = string.Empty;

    /// <summary>Must match the scopes of the released app version.</summary>
    public string Scopes { get; set; } = "read_orders,read_all_orders,read_products";

    public string ApiVersion { get; set; } = "2026-10";

    /// <summary>Where the CRM is opened, e.g. "https://crm.example.com"; Shopify sends the install back here.</summary>
    public string PublicUrl { get; set; } = string.Empty;

    public int SyncIntervalMinutes { get; set; } = 60;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && ShopifyRequests.IsShopDomain(Shop)
        && !string.IsNullOrWhiteSpace(PublicUrl);

    public string CallbackUrl => PublicUrl.TrimEnd('/') + "/api/shopify/callback";
}

/// <summary>Checks on what Shopify sends to the install and callback URLs.</summary>
public static class ShopifyRequests
{
    private static readonly Regex ShopDomain = new(@"^[a-z0-9][a-z0-9\-]*\.myshopify\.com$", RegexOptions.IgnoreCase);

    public static bool IsShopDomain(string? shop) => shop != null && ShopDomain.IsMatch(shop);

    /// <summary>
    /// Shopify signs install and callback requests: every query parameter but "hmac", sorted by
    /// name and joined as name=value with "&amp;", HMAC-SHA256 with the app's client secret, in hex.
    /// </summary>
    public static bool HasValidHmac(IEnumerable<KeyValuePair<string, string>> query, string clientSecret)
    {
        var parameters = query.ToList();
        var hmac = parameters.FirstOrDefault(p => p.Key == "hmac").Value;
        if (string.IsNullOrEmpty(hmac) || string.IsNullOrEmpty(clientSecret))
            return false;

        var message = string.Join("&", parameters
            .Where(p => p.Key is not ("hmac" or "signature"))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}"));

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(message));
        byte[] given;
        try
        {
            given = Convert.FromHexString(hmac);
        }
        catch (FormatException)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(expected, given);
    }

    public static string AuthorizeUrl(ShopifyOptions options, string state) =>
        $"https://{options.Shop}/admin/oauth/authorize"
        + $"?client_id={Uri.EscapeDataString(options.ClientId)}"
        + $"&scope={Uri.EscapeDataString(options.Scopes)}"
        + $"&redirect_uri={Uri.EscapeDataString(options.CallbackUrl)}"
        + $"&state={Uri.EscapeDataString(state)}";
}

public class ShopifyClient : IShopifyClient
{
    public const string HttpClientName = "Shopify";

    // Kept well under Shopify's 1000-point query cost: 50 orders × 20 lines
    private const int OrdersPerPage = 50;
    private const int LinesPerOrder = 20;
    private const int MaxThrottleRetries = 5;

    private const string OrdersQuery = """
        query Orders($first: Int!, $after: String, $query: String) {
          orders(first: $first, after: $after, query: $query, sortKey: UPDATED_AT) {
            pageInfo { hasNextPage endCursor }
            nodes {
              id
              name
              createdAt
              updatedAt
              cancelledAt
              test
              currentSubtotalPriceSet { shopMoney { amount currencyCode } }
              lineItems(first: 20) { nodes { title currentQuantity } }
            }
          }
        }
        """;

    private const string TimeZoneQuery = "query { shop { ianaTimezone } }";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ShopifyOptions _options;
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<ShopifyClient> _logger;

    public ShopifyClient(IOptions<ShopifyOptions> options, IHttpClientFactory httpClients, ILogger<ShopifyClient> logger)
    {
        _options = options.Value;
        _httpClients = httpClients;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public string Shop => _options.Shop.Trim().ToLowerInvariant();

    public Task<ShopifyToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code
        }, "exchange the install code", cancellationToken);

    public Task<ShopifyToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        }, "renew the access token", cancellationToken);

    public async Task<string> GetTimeZoneAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var data = await GraphQlAsync(accessToken, TimeZoneQuery, null, cancellationToken);
        return data.GetProperty("shop").GetProperty("ianaTimezone").GetString() ?? "UTC";
    }

    public async Task<ShopifyOrderPage> GetOrdersAsync(
        string accessToken, DateTime? changedSince, string? after, CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, object?>
        {
            ["first"] = OrdersPerPage,
            ["after"] = after,
            ["query"] = changedSince.HasValue
                ? $"updated_at:>='{changedSince.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}'"
                : null
        };

        var orders = (await GraphQlAsync(accessToken, OrdersQuery, variables, cancellationToken)).GetProperty("orders");
        var pageInfo = orders.GetProperty("pageInfo");

        return new ShopifyOrderPage(
            orders.GetProperty("nodes").EnumerateArray().Select(ReadOrder).ToList(),
            pageInfo.GetProperty("hasNextPage").GetBoolean(),
            pageInfo.GetProperty("endCursor").GetString());
    }

    private ShopifyOrder ReadOrder(JsonElement node)
    {
        var money = node.GetProperty("currentSubtotalPriceSet").GetProperty("shopMoney");
        var lines = node.GetProperty("lineItems").GetProperty("nodes").EnumerateArray()
            .Select(line => new OnlineOrderLineSnapshot(
                line.GetProperty("title").GetString() ?? string.Empty,
                line.GetProperty("currentQuantity").GetInt32()))
            .ToList();
        if (lines.Count == LinesPerOrder)
            _logger.LogWarning("Order {Name} has {Count}+ lines; only the first are read", node.GetProperty("name"), LinesPerOrder);

        return new ShopifyOrder(
            node.GetProperty("id").GetString()!,
            node.GetProperty("name").GetString() ?? string.Empty,
            node.GetProperty("createdAt").GetDateTime().ToUniversalTime(),
            node.GetProperty("updatedAt").GetDateTime().ToUniversalTime(),
            node.GetProperty("cancelledAt").ValueKind != JsonValueKind.Null,
            node.GetProperty("test").GetBoolean(),
            decimal.Parse(money.GetProperty("amount").GetString()!, CultureInfo.InvariantCulture),
            money.GetProperty("currencyCode").GetString() ?? string.Empty,
            lines);
    }

    private async Task<JsonElement> GraphQlAsync(
        string accessToken, string query, object? variables, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var url = $"https://{Shop}/admin/api/{_options.ApiVersion}/graphql.json";

        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new { query, variables })
            };
            request.Headers.Add("X-Shopify-Access-Token", accessToken);

            using var response = await SendAsync(request, "read from the Admin API", cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);

            if (body.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                // The query budget refills within seconds; anything else is a real failure
                if (IsThrottled(errors) && attempt < MaxThrottleRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * (attempt + 1)), cancellationToken);
                    continue;
                }
                throw new ShopifyUnavailableException("Shopify refused the query: " + string.Join("; ",
                    errors.EnumerateArray().Select(e => e.TryGetProperty("message", out var m) ? m.GetString() : e.ToString())));
            }

            return body.GetProperty("data");
        }
    }

    private static bool IsThrottled(JsonElement errors) => errors.EnumerateArray().Any(e =>
        e.TryGetProperty("extensions", out var ext)
        && ext.TryGetProperty("code", out var code)
        && code.GetString() == "THROTTLED");

    private async Task<ShopifyToken> RequestTokenAsync(
        Dictionary<string, string> form, string what, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{Shop}/admin/oauth/access_token")
        {
            Content = JsonContent.Create(form)
        };

        using var response = await SendAsync(request, what, cancellationToken);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, cancellationToken)
                    ?? throw new ShopifyUnavailableException($"Shopify sent no token when asked to {what}");

        return new ShopifyToken(
            token.AccessToken,
            token.Scope,
            token.ExpiresIn is > 0 ? DateTime.UtcNow.AddSeconds(token.ExpiresIn.Value) : null,
            string.IsNullOrEmpty(token.RefreshToken) ? null : token.RefreshToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string what, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClients.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ShopifyUnavailableException($"Shopify could not be reached to {what}: {ex.Message}", ex);
        }

        if (response.IsSuccessStatusCode)
            return response;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        response.Dispose();
        _logger.LogWarning("Shopify answered {Status} when asked to {What}: {Detail}", (int)response.StatusCode, what, detail);
        throw new ShopifyUnavailableException(
            $"Shopify answered {(int)response.StatusCode} when asked to {what}" + (detail.Length > 0 ? $": {Truncate(detail)}" : ""));
    }

    private static string Truncate(string text) => text.Length > 300 ? text[..300] : text;

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new ShopifyUnavailableException("The Shopify app is not configured");
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}

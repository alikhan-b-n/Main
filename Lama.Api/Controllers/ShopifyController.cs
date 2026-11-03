using System.Security.Cryptography;
using Lama.Application.OnlineSales;
using Lama.Domain.AccessControl.Entities;
using Lama.Infrastructure.Integrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lama.Api.Controllers;

/// <summary>
/// Installing the Shopify app and the copy of website orders it allows.
///
/// Install (Shopify's authorization code flow): the install link sends the browser to
/// GET install, which forwards it to Shopify to approve the scopes; Shopify then returns
/// it to GET callback with a code, which is traded for the store's access token. Both are
/// open to anyone, since Shopify cannot sign in to the CRM: the hmac Shopify signs each
/// request with, the configured store and a one-time state guard them instead.
/// </summary>
[ApiController]
[Route("api/shopify")]
public class ShopifyController : ControllerBase
{
    private const string StateCookie = "shopify_oauth_state";

    private readonly IMediator _mediator;
    private readonly ShopifyOptions _options;
    private readonly ShopifySyncSignal _signal;
    private readonly ILogger<ShopifyController> _logger;

    public ShopifyController(
        IMediator mediator, IOptions<ShopifyOptions> options, ShopifySyncSignal signal, ILogger<ShopifyController> logger)
    {
        _mediator = mediator;
        _options = options.Value;
        _signal = signal;
        _logger = logger;
    }

    /// <summary>Where the install link and the app's entry in the Shopify admin land.</summary>
    [AllowAnonymous]
    [HttpGet("install")]
    public IActionResult Install([FromQuery] string? shop)
    {
        if (!_options.IsConfigured)
            return NotFound(new { message = "The Shopify app is not configured" });
        if (!IsOurShop(shop))
            return BadRequest(new { message = "This app can only be installed on the bookseller's own store" });
        if (Request.Query.ContainsKey("hmac") && !HasValidHmac())
            return BadRequest(new { message = "The request was not signed by Shopify" });

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        Response.Cookies.Append(StateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            // Behind Caddy and nginx the request arrives as http; the public address says what the browser uses
            Secure = _options.PublicUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            // Lax: the cookie must come back on Shopify's redirect to the callback
            SameSite = SameSiteMode.Lax,
            Path = "/api/shopify",
            MaxAge = TimeSpan.FromMinutes(10)
        });

        return Redirect(ShopifyRequests.AuthorizeUrl(_options, state));
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? shop, [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            return NotFound(new { message = "The Shopify app is not configured" });
        if (!IsOurShop(shop) || !HasValidHmac())
            return BadRequest(new { message = "The request was not signed by Shopify" });

        var expected = Request.Cookies[StateCookie];
        Response.Cookies.Delete(StateCookie, new CookieOptions { Path = "/api/shopify" });
        if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expected) || !FixedTimeEquals(state, expected))
            return BadRequest(new { message = "The install link has expired; open it again from Shopify" });
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Shopify sent no code" });

        try
        {
            await _mediator.Send(new ConnectShopifyCommand(code), cancellationToken);
        }
        catch (ShopifyUnavailableException ex)
        {
            _logger.LogError(ex, "Shopify install could not be completed");
            return Redirect(FinancialsUrl("failed"));
        }

        // The first copy of the order history starts right away rather than within the hour
        _signal.Request();
        return Redirect(FinancialsUrl("connected"));
    }

    /// <summary>Whether the store is connected and how the last sync went.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ShopifyStatusDto>> GetStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetShopifyStatusQuery(), cancellationToken));

    /// <summary>Reads new and changed orders now.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("sync")]
    public async Task<ActionResult<ShopifySyncResult>> Sync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SyncShopifyOrdersCommand(), cancellationToken);
        return result.Outcome switch
        {
            ShopifySyncOutcome.NotConnected => Conflict(new { message = "The Shopify app is not installed yet" }),
            ShopifySyncOutcome.Failed => StatusCode(StatusCodes.Status502BadGateway, result),
            _ => Ok(result)
        };
    }

    private bool IsOurShop(string? shop) =>
        ShopifyRequests.IsShopDomain(shop)
        && string.Equals(shop, _options.Shop.Trim(), StringComparison.OrdinalIgnoreCase);

    private bool HasValidHmac() => ShopifyRequests.HasValidHmac(
        Request.Query.Select(p => new KeyValuePair<string, string>(p.Key, p.Value.ToString())),
        _options.ClientSecret);

    private string FinancialsUrl(string outcome) => $"{_options.PublicUrl.TrimEnd('/')}/financials?shopify={outcome}";

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(a), System.Text.Encoding.UTF8.GetBytes(b));
}

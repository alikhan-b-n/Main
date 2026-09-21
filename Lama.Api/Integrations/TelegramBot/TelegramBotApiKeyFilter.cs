using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Lama.Api.Integrations.TelegramBot;

public class TelegramBotOptions
{
    public const string SectionName = "Integrations:TelegramBot";

    /// <summary>
    /// Shared secret the bot sends as "Authorization: Bearer &lt;key&gt;" (its CRM_API_KEY).
    /// Keep it out of appsettings.json: use user-secrets or the
    /// Integrations__TelegramBot__ApiKey environment variable.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Guards the intake endpoint. The rest of the CRM has no auth yet, but this endpoint
/// is called by an external service, so it must never be open — an unset key disables it.
/// </summary>
public class TelegramBotApiKeyFilter : IAuthorizationFilter
{
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramBotApiKeyFilter> _logger;

    public TelegramBotApiKeyFilter(IOptions<TelegramBotOptions> options, ILogger<TelegramBotApiKeyFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Telegram bot intake called, but {Section}:ApiKey is not configured", TelegramBotOptions.SectionName);
            context.Result = new ObjectResult(new { message = "Telegram bot integration is not configured" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var provided = ReadKey(context.HttpContext.Request);
        if (provided == null || !FixedTimeEquals(provided, _options.ApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid or missing API key" });
        }
    }

    private static string? ReadKey(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        var apiKey = request.Headers["X-Api-Key"].ToString();
        return string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

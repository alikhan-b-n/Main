using System.Net.Http.Json;
using System.Text.Json;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Exceptions;
using Lama.Integrations.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Integrations.AI.Clients;

/// <summary>
/// Calls Ollama's native /api/generate endpoint. System prompt and user
/// message are concatenated into a single prompt (Ollama generate API has
/// no structured chat schema on the generate endpoint, and we keep the
/// historical request shape so existing Ollama installs continue to work).
/// </summary>
public class OllamaCompletionClient : IAiCompletionClient
{
    public const string HttpClientName = "OllamaAi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OllamaSettings _defaults;
    private readonly ILogger<OllamaCompletionClient> _logger;

    public OllamaCompletionClient(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaSettings> defaults,
        ILogger<OllamaCompletionClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _defaults = defaults.Value;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(
        AiProviderOptions options,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(options.OllamaBaseUrl)
            ? options.OllamaBaseUrl!
            : _defaults.BaseUrl;
        var model = !string.IsNullOrWhiteSpace(options.OllamaModel)
            ? options.OllamaModel!
            : _defaults.Model;

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(_defaults.TimeoutSeconds);

        var prompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? userMessage
            : $"{systemPrompt}\n\n{userMessage}";

        var requestBody = new
        {
            model,
            prompt,
            stream = false,
            options = new
            {
                temperature = _defaults.Temperature,
                num_predict = _defaults.MaxTokens
            }
        };

        _logger.LogInformation("Calling Ollama at {BaseUrl} with model {Model}", baseUrl, model);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/generate", requestBody, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Ollama HTTP call failed at {BaseUrl}", baseUrl);
            throw new AiProviderException("ollama", $"Ollama provider request failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBody(response, cancellationToken);
            _logger.LogError("Ollama returned {Status}: {Body}", response.StatusCode, body);
            throw new AiProviderException(
                "ollama",
                $"Ollama provider returned HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<OllamaResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return parsed?.Response?.Trim();
    }

    private static async Task<string> SafeReadBody(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }

    private class OllamaResponse
    {
        public string? Response { get; set; }
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Exceptions;
using Lama.Integrations.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Integrations.AI.Clients;

/// <summary>
/// Shared client for OpenAI-compatible providers (LM Studio + Groq).
/// Calls POST {baseUrl}/v1/chat/completions (or {baseUrl}/chat/completions
/// when baseUrl already ends with /v1).
/// </summary>
public class OpenAiCompatibleCompletionClient : IAiCompletionClient
{
    public const string HttpClientName = "OpenAiCompatibleAi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LmStudioSettings _lmStudioDefaults;
    private readonly GroqSettings _groqDefaults;
    private readonly ILogger<OpenAiCompatibleCompletionClient> _logger;

    public OpenAiCompatibleCompletionClient(
        IHttpClientFactory httpClientFactory,
        IOptions<LmStudioSettings> lmStudioDefaults,
        IOptions<GroqSettings> groqDefaults,
        ILogger<OpenAiCompatibleCompletionClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _lmStudioDefaults = lmStudioDefaults.Value;
        _groqDefaults = groqDefaults.Value;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(
        AiProviderOptions options,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var provider = (options.Provider ?? string.Empty).ToLowerInvariant();
        string baseUrl;
        string model;
        string? apiKey;
        int timeoutSeconds;

        switch (provider)
        {
            case "lmstudio":
                baseUrl = !string.IsNullOrWhiteSpace(options.LmStudioBaseUrl)
                    ? options.LmStudioBaseUrl!
                    : _lmStudioDefaults.BaseUrl;
                model = options.LmStudioModel ?? _lmStudioDefaults.Model ?? string.Empty;
                apiKey = null; // LM Studio: no auth by default
                timeoutSeconds = _lmStudioDefaults.TimeoutSeconds;
                break;

            case "groq":
                if (string.IsNullOrWhiteSpace(options.GroqApiKey))
                    throw new AiProviderConfigurationException("Groq API key is required");
                baseUrl = _groqDefaults.BaseUrl;
                model = options.GroqModel ?? string.Empty;
                apiKey = options.GroqApiKey;
                timeoutSeconds = _groqDefaults.TimeoutSeconds;
                break;

            default:
                throw new AiProviderConfigurationException(
                    $"Unknown AI provider: {options.Provider}");
        }

        var requestUri = BuildChatCompletionsUri(baseUrl);
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var requestBody = new ChatCompletionRequest
        {
            Model = model,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userMessage }
            },
            Temperature = 0.2,
            Stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(requestBody)
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        _logger.LogInformation(
            "Calling OpenAI-compatible provider {Provider} at {Uri} with model {Model}",
            provider, requestUri, model);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "{Provider} HTTP call failed at {Uri}", provider, requestUri);
            throw new AiProviderException(provider, $"{provider} provider request failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBody(response, cancellationToken);
            _logger.LogError(
                "{Provider} returned {Status}: {Body}",
                provider, response.StatusCode, body);
            throw new AiProviderException(
                provider,
                $"{provider} provider returned HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return parsed?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
    }

    /// <summary>
    /// Builds the chat completions URL. If <paramref name="baseUrl"/> already
    /// ends with /v1 (with or without trailing slash) we append only
    /// /chat/completions; otherwise we append /v1/chat/completions.
    /// </summary>
    private static string BuildChatCompletionsUri(string baseUrl)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{trimmed}/chat/completions"
            : $"{trimmed}/v1/chat/completions";
    }

    private static async Task<string> SafeReadBody(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }

    private class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public ChatCompletionChoice[]? Choices { get; set; }
    }

    private class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}

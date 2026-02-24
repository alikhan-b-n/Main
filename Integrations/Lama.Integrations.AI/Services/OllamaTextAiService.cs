using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Integrations.AI.Services;

/// <summary>
/// AI text service implementation using Ollama (local LLM).
/// Ollama is a free, local AI solution that runs models like Llama 3, Mistral, etc.
/// No API key required!
/// </summary>
public class OllamaTextAiService : ITextAiService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaTextAiService> _logger;

    public OllamaTextAiService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaTextAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<string> SummarizeAsync(string? subject, string? body, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build the prompt
            var prompt = BuildSummarizationPrompt(subject, body);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return string.Empty;
            }

            // Call Ollama API
            var response = await GenerateTextAsync(prompt, cancellationToken);

            return response ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to summarize text using Ollama");

            // Fallback to simple heuristic if Ollama is unavailable
            return FallbackSummarization(subject, body);
        }
    }

    private string BuildSummarizationPrompt(string? subject, string? body)
    {
        if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var content = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(subject))
        {
            content.AppendLine($"Subject: {subject}");
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            content.AppendLine($"Body: {body}");
        }

        return $@"Please provide a concise summary of the following activity in 1-2 sentences.
Focus on the key points and action items.

{content}

Summary:";
    }

    private async Task<string?> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _settings.Model,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = _settings.Temperature,
                num_predict = _settings.MaxTokens
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Ollama API with model: {Model}", _settings.Model);

        var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Ollama API returned error: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        // Deserialize with case-insensitive options
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, options);

        return result?.Response?.Trim();
    }

    private string FallbackSummarization(string? subject, string? body)
    {
        _logger.LogWarning("Using fallback summarization (Ollama unavailable)");

        if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            return subject.Trim();

        var combined = (subject + "\n\n" + (body ?? string.Empty)).Trim();

        // Try to extract the first sentence from the body
        var sentences = body?.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));

        if (sentences != null && sentences.Any())
        {
            var first = sentences.First();
            var result = !string.IsNullOrWhiteSpace(subject) ? subject.Trim() + " — " + first : first;
            if (result.Length <= 500) return result;
        }

        // Fallback: take first 200 characters of combined text
        var snippet = combined.Length <= 200 ? combined : combined.Substring(0, 197) + "...";
        return snippet;
    }

    // Response model for Ollama API
    private class OllamaResponse
    {
        public string? Model { get; set; }
        public string? Response { get; set; }
        public bool Done { get; set; }
    }
}
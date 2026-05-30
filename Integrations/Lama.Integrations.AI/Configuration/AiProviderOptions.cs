namespace Lama.Integrations.AI.Configuration;

/// <summary>
/// Request-scoped options describing which AI provider to use for a single
/// scenario call. Built from the incoming HTTP request payload.
/// When <see cref="Provider"/> is null/empty the system falls back to the
/// configured default Ollama settings (legacy behavior).
/// </summary>
public class AiProviderOptions
{
    /// <summary>
    /// Provider identifier: "ollama" | "lmstudio" | "groq".
    /// Case-insensitive. Null/empty = default (Ollama from configuration).
    /// </summary>
    public string? Provider { get; set; }

    // Ollama
    public string? OllamaBaseUrl { get; set; }
    public string? OllamaModel { get; set; }

    // LM Studio
    public string? LmStudioBaseUrl { get; set; }
    public string? LmStudioModel { get; set; }

    // Groq
    public string? GroqApiKey { get; set; }
    public string? GroqModel { get; set; }

    /// <summary>
    /// Optional system-prompt override. When non-empty, replaces the
    /// hardcoded default system prompt for the scenario.
    /// </summary>
    public string? CustomPrompt { get; set; }
}

namespace Lama.Integrations.AI.Configuration;

/// <summary>
/// Configuration settings for Ollama local LLM integration.
/// </summary>
public class OllamaSettings
{
    public const string SectionName = "Ollama";

    /// <summary>
    /// Base URL for Ollama API. Default: http://localhost:11434
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model to use for text generation. Examples: llama3, mistral, codellama
    /// </summary>
    public string Model { get; set; } = "llama3";

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for generation (0.0 to 1.0). Lower = more deterministic
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
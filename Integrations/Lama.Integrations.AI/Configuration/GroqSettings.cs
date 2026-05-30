namespace Lama.Integrations.AI.Configuration;

/// <summary>
/// Default settings for the Groq cloud provider (OpenAI-compatible).
/// API key is NEVER configured here — it must come from the request.
/// </summary>
public class GroqSettings
{
    public const string SectionName = "Groq";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    public int TimeoutSeconds { get; set; } = 60;
}

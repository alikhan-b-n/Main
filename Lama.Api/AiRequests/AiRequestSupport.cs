using System.Text.Json.Serialization;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.AiRequests;

/// <summary>
/// Shared fields the four AI endpoints accept from the frontend. POST
/// endpoints embed this via composition / wrapper DTOs; GET endpoints
/// accept it as [FromQuery]. JsonPropertyName is explicit on every field
/// so the wire contract is independent of the host JsonNamingPolicy and
/// the lmstudio* keys bind exactly as the frontend sends them.
/// </summary>
public class AiRequestFields
{
    [JsonPropertyName("aiProvider")]
    public string? AiProvider { get; set; }

    [JsonPropertyName("ollamaBaseUrl")]
    public string? OllamaBaseUrl { get; set; }

    [JsonPropertyName("ollamaModel")]
    public string? OllamaModel { get; set; }

    [JsonPropertyName("lmstudioBaseUrl")]
    public string? LmStudioBaseUrl { get; set; }

    [JsonPropertyName("lmstudioModel")]
    public string? LmStudioModel { get; set; }

    [JsonPropertyName("groqApiKey")]
    public string? GroqApiKey { get; set; }

    [JsonPropertyName("groqModel")]
    public string? GroqModel { get; set; }

    [JsonPropertyName("customPrompt")]
    public string? CustomPrompt { get; set; }

    public AiProviderOptions ToProviderOptions() => new()
    {
        Provider = AiProvider,
        OllamaBaseUrl = OllamaBaseUrl,
        OllamaModel = OllamaModel,
        LmStudioBaseUrl = LmStudioBaseUrl,
        LmStudioModel = LmStudioModel,
        GroqApiKey = GroqApiKey,
        GroqModel = GroqModel,
        CustomPrompt = CustomPrompt,
    };
}

public static class AiRequestExceptionHandling
{
    /// <summary>
    /// Translates AI integration exceptions into HTTP responses:
    /// AiProviderConfigurationException → 400, AiProviderException → 502.
    /// Returns null if no translation was needed (caller continues).
    /// </summary>
    public static IActionResult? TryHandle(Exception ex, ILogger logger)
    {
        switch (ex)
        {
            case AiProviderConfigurationException configEx:
                return new BadRequestObjectResult(new { error = configEx.Message });

            case AiProviderException providerEx:
                logger.LogError(providerEx, "AI provider {Provider} call failed", providerEx.Provider);
                return new ObjectResult(new
                {
                    error = $"AI provider '{providerEx.Provider}' is unavailable or returned an error."
                })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };

            default:
                return null;
        }
    }
}

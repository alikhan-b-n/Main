using Lama.Integrations.AI.Configuration;

namespace Lama.Integrations.AI.Interfaces;

/// <summary>
/// Low-level transport-aware client that performs a single chat completion.
/// Implementations target a specific provider (Ollama native, OpenAI-compatible).
/// </summary>
public interface IAiCompletionClient
{
    Task<string?> CompleteAsync(
        AiProviderOptions options,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves an <see cref="IAiCompletionClient"/> based on the request-scoped
/// <see cref="AiProviderOptions"/>.
/// </summary>
public interface IAiCompletionClientFactory
{
    IAiCompletionClient GetClient(AiProviderOptions options);
}

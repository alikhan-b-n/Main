using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Exceptions;
using Lama.Integrations.AI.Interfaces;

namespace Lama.Integrations.AI.Clients;

public class AiCompletionClientFactory : IAiCompletionClientFactory
{
    private readonly OllamaCompletionClient _ollama;
    private readonly OpenAiCompatibleCompletionClient _openAiCompatible;

    public AiCompletionClientFactory(
        OllamaCompletionClient ollama,
        OpenAiCompatibleCompletionClient openAiCompatible)
    {
        _ollama = ollama;
        _openAiCompatible = openAiCompatible;
    }

    public IAiCompletionClient GetClient(AiProviderOptions options)
    {
        var provider = (options.Provider ?? string.Empty).Trim().ToLowerInvariant();

        return provider switch
        {
            "" or "ollama" => _ollama,
            "lmstudio"     => _openAiCompatible,
            "groq"         => _openAiCompatible,
            _ => throw new AiProviderConfigurationException($"Unknown AI provider: {options.Provider}")
        };
    }
}

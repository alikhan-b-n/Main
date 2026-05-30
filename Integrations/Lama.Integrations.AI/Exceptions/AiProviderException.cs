namespace Lama.Integrations.AI.Exceptions;

/// <summary>
/// Thrown when an AI provider HTTP call fails (network error or non-2xx
/// response). Controllers translate this into HTTP 502 Bad Gateway.
/// </summary>
public class AiProviderException : Exception
{
    public string Provider { get; }

    public AiProviderException(string provider, string message)
        : base(message)
    {
        Provider = provider;
    }

    public AiProviderException(string provider, string message, Exception inner)
        : base(message, inner)
    {
        Provider = provider;
    }
}

/// <summary>
/// Thrown when the request specifies an invalid / unsupported provider
/// configuration (unknown provider name, missing required field).
/// Controllers translate this into HTTP 400 Bad Request.
/// </summary>
public class AiProviderConfigurationException : Exception
{
    public AiProviderConfigurationException(string message) : base(message) { }
}

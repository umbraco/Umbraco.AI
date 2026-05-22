namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Shared HTTP-status → <see cref="AIProviderErrorCategory"/> mapping used by the built-in
/// classifiers and available to provider packages so OpenAI-compatible and provider-specific
/// classifiers stay consistent.
/// </summary>
public static class ProviderErrorMapping
{
    /// <summary>
    /// Maps an HTTP status code to a populated <see cref="AIProviderErrorInfo"/>.
    /// </summary>
    /// <param name="status">The HTTP status code from the provider response.</param>
    /// <param name="rawMessage">The original exception message for diagnostics.</param>
    /// <param name="providerCode">Optional provider-specific code (e.g. <c>overloaded_error</c>).</param>
    public static AIProviderErrorInfo FromHttpStatus(int status, string rawMessage, string? providerCode = null)
        => status switch
        {
            401 or 403 => new(AIProviderErrorCategory.Authentication,
                "Authentication failed. Check the connection's credentials.",
                providerCode ?? status.ToString(), rawMessage),

            404 => new(AIProviderErrorCategory.NotFound,
                "The requested AI model or resource was not found.",
                providerCode ?? "404", rawMessage),

            408 or 504 => new(AIProviderErrorCategory.Transient,
                "The AI service took too long to respond. Try again in a moment.",
                providerCode ?? status.ToString(), rawMessage),

            429 => new(AIProviderErrorCategory.RateLimited,
                "Rate limit reached. Please wait a moment and try again.",
                providerCode ?? "429", rawMessage),

            // 529 is Anthropic's overload-specific status; retry semantics match 503.
            502 or 503 or 529 => new(AIProviderErrorCategory.Transient,
                "The AI service is briefly unavailable. Try again in a few seconds.",
                providerCode ?? status.ToString(), rawMessage),

            >= 500 and < 600 => new(AIProviderErrorCategory.Transient,
                "The AI service returned an error. Try again in a moment.",
                providerCode ?? status.ToString(), rawMessage),

            >= 400 and < 500 => new(AIProviderErrorCategory.InvalidRequest,
                "The AI service rejected the request.",
                providerCode ?? status.ToString(), rawMessage),

            _ => new(AIProviderErrorCategory.Unknown,
                "An unexpected error occurred talking to the AI service.",
                providerCode ?? status.ToString(), rawMessage),
        };
}

using System.Text.Json;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Anthropic.Errors;

/// <summary>
/// Classifies exceptions thrown by the Anthropic .NET SDK, including the mid-stream SSE
/// <c>overloaded_error</c> case that motivated this work (issue #174).
/// </summary>
/// <remarks>
/// <para>
/// The Anthropic SDK surfaces SSE error events by throwing an exception whose <c>Message</c>
/// embeds the raw JSON envelope, e.g.
/// </para>
/// <code>
/// SSE error returned from server: '{"type":"error","error":{"type":"overloaded_error",
/// "message":"Overloaded"},"request_id":"req_..."}'
/// </code>
/// <para>
/// HTTP-level errors are mapped via <see cref="ProviderErrorMapping"/>; mid-stream SSE errors
/// are parsed from the message JSON to preserve Anthropic's <c>error.type</c> code.
/// </para>
/// </remarks>
internal sealed class AnthropicProviderErrorClassifier : IAIProviderErrorClassifier
{
    private const string AnthropicNamespacePrefix = "Anthropic";

    /// <inheritdoc />
    public AIProviderErrorInfo? Classify(Exception exception)
    {
        if (!IsAnthropicException(exception))
        {
            return null;
        }

        // SSE error events come through as a string-encoded JSON envelope in the Message.
        var sseInfo = TryClassifySseEnvelope(exception);
        if (sseInfo is not null)
        {
            return sseInfo;
        }

        // Fall back to HTTP status (the SDK exposes Status / StatusCode on its ApiException types).
        var status = TryGetHttpStatus(exception);
        if (status is not null)
        {
            return ProviderErrorMapping.FromHttpStatus(status.Value, exception.Message);
        }

        // Recognised as Anthropic but unable to extract structure — surface a generic message
        // rather than the raw exception text.
        return new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            "The Anthropic service returned an unexpected error. Please try again.",
            ProviderCode: null,
            exception.Message);
    }

    /// <summary>
    /// True when the exception (or any inner exception) originates from the Anthropic SDK.
    /// Matched by namespace prefix so we don't take an SDK type dependency in Core.
    /// </summary>
    private static bool IsAnthropicException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var ns = current.GetType().Namespace;
            if (ns is not null && ns.StartsWith(AnthropicNamespacePrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the JSON envelope embedded in an SSE error message and maps the <c>error.type</c>
    /// to a normalised category.
    /// </summary>
    private static AIProviderErrorInfo? TryClassifySseEnvelope(Exception exception)
    {
        var message = exception.Message;
        if (string.IsNullOrEmpty(message))
        {
            return null;
        }

        var jsonStart = message.IndexOf('{');
        var jsonEnd = message.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return null;
        }

        var json = message.Substring(jsonStart, jsonEnd - jsonStart + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("error", out var error) ||
                error.ValueKind != JsonValueKind.Object ||
                !error.TryGetProperty("type", out var typeElement) ||
                typeElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var errorType = typeElement.GetString() ?? string.Empty;
            return MapAnthropicErrorType(errorType, exception.Message);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AIProviderErrorInfo MapAnthropicErrorType(string errorType, string rawMessage) => errorType switch
    {
        "overloaded_error" => new(AIProviderErrorCategory.Transient,
            "The AI service is briefly overloaded. Please try again in a few seconds.",
            errorType, rawMessage),

        "rate_limit_error" => new(AIProviderErrorCategory.RateLimited,
            "Rate limit reached. Please wait a moment and try again.",
            errorType, rawMessage),

        "authentication_error" or "permission_error" => new(AIProviderErrorCategory.Authentication,
            "Authentication failed. Check the connection's API key.",
            errorType, rawMessage),

        "not_found_error" => new(AIProviderErrorCategory.NotFound,
            "The requested model or resource was not found.",
            errorType, rawMessage),

        "invalid_request_error" => new(AIProviderErrorCategory.InvalidRequest,
            "The request was rejected by the AI service.",
            errorType, rawMessage),

        "api_error" => new(AIProviderErrorCategory.Transient,
            "The AI service returned an error. Please try again in a moment.",
            errorType, rawMessage),

        _ => new(AIProviderErrorCategory.Unknown,
            "The Anthropic service returned an unexpected error. Please try again.",
            errorType, rawMessage),
    };

    /// <summary>
    /// Reads <c>StatusCode</c> from any exception in the chain via duck-typing.
    /// </summary>
    /// <remarks>
    /// The Anthropic SDK exposes <c>StatusCode</c> on its <c>ApiException</c> types, but the
    /// concrete class lives behind generated code that may change between SDK versions. Looking up
    /// the property by name keeps this classifier independent of those internals.
    /// </remarks>
    private static int? TryGetHttpStatus(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var prop = current.GetType().GetProperty("StatusCode")
                       ?? current.GetType().GetProperty("Status");
            if (prop is null) continue;

            var raw = prop.GetValue(current);
            switch (raw)
            {
                case int i when i > 0:
                    return i;
                case System.Net.HttpStatusCode hsc when (int)hsc > 0:
                    return (int)hsc;
            }
        }

        return null;
    }
}

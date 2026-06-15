using System.Text.Json;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Anthropic.Errors;

/// <summary>
/// Maps Anthropic SDK error shapes that the shared <see cref="ProviderErrorMapping"/> doesn't
/// recognise — in particular the mid-stream SSE <c>overloaded_error</c> envelope that motivated
/// this work (issue #174).
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
/// This is invoked by <see cref="AnthropicProvider.ClassifyError"/>, which is only ever called for
/// exceptions Anthropic produced — so there is no need to sniff the exception's namespace. Returns
/// <c>null</c> when no Anthropic-specific structure is found, leaving the caller to fall back to the
/// shared mapping.
/// </para>
/// </remarks>
internal static class AnthropicErrorMapping
{
    /// <summary>
    /// Attempts to classify an Anthropic SDK exception from its SSE error envelope or HTTP status.
    /// </summary>
    /// <returns>The classified info, or <c>null</c> if no Anthropic-specific structure was found.</returns>
    public static AIProviderErrorInfo? TryClassify(Exception exception)
    {
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

        return null;
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
    /// Reads <c>StatusCode</c>/<c>Status</c> from any exception in the chain via duck-typing.
    /// </summary>
    /// <remarks>
    /// The Anthropic SDK exposes the status on its <c>ApiException</c> types, but the concrete class
    /// lives behind generated code that may change between SDK versions. Looking up the property by
    /// name keeps this independent of those internals.
    /// </remarks>
    private static int? TryGetHttpStatus(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var prop = current.GetType().GetProperty("StatusCode")
                       ?? current.GetType().GetProperty("Status");
            if (prop is null)
            {
                continue;
            }

            switch (prop.GetValue(current))
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

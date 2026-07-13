using System.ClientModel;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Shared exception → <see cref="AIProviderErrorInfo"/> mapping. Provides the default
/// classification used by <see cref="AIProviderBase.ClassifyError"/> and an HTTP-status helper
/// that provider packages can reuse so OpenAI-compatible and provider-specific mappings stay
/// consistent.
/// </summary>
public static class ProviderErrorMapping
{
    /// <summary>
    /// The default classification for an exception, walking the inner-exception chain and
    /// recognising the common transport types: <see cref="ClientResultException"/> (the OpenAI SDK
    /// family), <see cref="HttpRequestException"/>, cancellation, and timeouts.
    /// </summary>
    /// <remarks>
    /// Providers override <see cref="AIProviderBase.ClassifyError"/> to handle SDK-specific error
    /// shapes, typically delegating here for anything they don't recognise.
    /// </remarks>
    /// <param name="exception">The exception to classify.</param>
    public static AIProviderErrorInfo FromException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case ClientResultException cre:
                    // Status == 0 means the request never made it to the server.
                    return cre.Status == 0
                        ? ClassifyTransportFailure(exception)
                        : FromHttpStatus(cre.Status, exception.Message, TryExtractOpenAIErrorCode(cre));

                // A TimeoutException surfacing as (or wrapped by) an OperationCanceledException is how
                // HttpClient reports its own request timeout, not a real cancellation - check it first
                // so genuine timeouts aren't misreported as "request was cancelled".
                case OperationCanceledException when HasInner<TimeoutException>(current):
                case TimeoutException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.Transient,
                        "The AI service took too long to respond. Try again in a moment.",
                        ProviderCode: "timeout",
                        exception.Message);

                case OperationCanceledException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.Cancelled,
                        "The request was cancelled.",
                        ProviderCode: null,
                        exception.Message);

                case HttpRequestException { StatusCode: { } code }:
                    return FromHttpStatus((int)code, exception.Message);

                case HttpRequestException:
                    return ClassifyTransportFailure(exception);
            }
        }

        return new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            "An unexpected error occurred talking to the AI service.",
            ProviderCode: null,
            exception.Message);
    }

    /// <summary>
    /// Differentiates a connectivity failure - one where the request never reached the server - by
    /// walking <paramref name="exception"/>'s inner-exception chain for a recognisable transport
    /// cause (DNS resolution, connection refused/unreachable, TLS handshake). Falls back to a generic
    /// network message when nothing more specific is recognised.
    /// </summary>
    /// <remarks>
    /// All outcomes stay in <see cref="AIProviderErrorCategory.NetworkError"/> - only the user-facing
    /// message and <see cref="AIProviderErrorInfo.ProviderCode"/> are differentiated, since every case
    /// here shares the same retry semantics (check connectivity/config, then try again).
    /// </remarks>
    private static AIProviderErrorInfo ClassifyTransportFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case AuthenticationException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Secure (TLS/SSL) connection to the AI service failed. Check certificates and the endpoint URL.",
                        ProviderCode: "tls",
                        exception.Message);

                case SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData }:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Couldn't resolve the AI service address. Check the endpoint/host and your DNS.",
                        ProviderCode: "dns",
                        exception.Message);

                case SocketException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Couldn't connect to the AI service. It may be down, unreachable, or blocked by a firewall.",
                        ProviderCode: "connection",
                        exception.Message);

                case HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError }:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Couldn't resolve the AI service address. Check the endpoint/host and your DNS.",
                        ProviderCode: "dns",
                        exception.Message);

                case HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError }:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Secure (TLS/SSL) connection to the AI service failed. Check certificates and the endpoint URL.",
                        ProviderCode: "tls",
                        exception.Message);

                case HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError }:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Couldn't connect to the AI service. It may be down, unreachable, or blocked by a firewall.",
                        ProviderCode: "connection",
                        exception.Message);
            }
        }

        return new AIProviderErrorInfo(
            AIProviderErrorCategory.NetworkError,
            "Couldn't reach the AI service. Check your connection and try again.",
            ProviderCode: null,
            exception.Message);
    }

    /// <summary>
    /// Whether <paramref name="exception"/>'s inner-exception chain contains a <typeparamref name="T"/>.
    /// </summary>
    private static bool HasInner<T>(Exception exception)
        where T : Exception
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is T)
            {
                return true;
            }
        }

        return false;
    }

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

    /// <summary>
    /// Best-effort extraction of an OpenAI-shaped error code (<c>{ "error": { "code": "..." } }</c>)
    /// from a <see cref="ClientResultException"/> response body. Returns null on any failure so the
    /// HTTP-status mapping still provides a fallback code.
    /// </summary>
    private static string? TryExtractOpenAIErrorCode(ClientResultException ex)
    {
        try
        {
            var content = ex.GetRawResponse()?.Content;
            if (content is null || content.ToMemory().IsEmpty)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(content.ToMemory());
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("code", out var code) &&
                code.ValueKind == JsonValueKind.String)
            {
                return code.GetString();
            }
        }
        catch
        {
            // Body not JSON, not a known shape, or already disposed. Fall through.
        }

        return null;
    }
}

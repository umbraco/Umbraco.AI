using System.ClientModel;
using System.Text.Json;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Classifies <see cref="ClientResultException"/> — the exception type thrown by the OpenAI .NET
/// SDK (and consequently DeepSeek, Fireworks AI, Hugging Face, Together AI, and the Azure-OpenAI
/// chat paths used by Microsoft Foundry).
/// </summary>
/// <remarks>
/// <para>
/// HTTP status is read from <see cref="ClientResultException.Status"/>. When the response body is
/// available it is also parsed for an OpenAI-style <c>{ "error": { "code": "..." } }</c> envelope
/// so the provider's own code (e.g. <c>insufficient_quota</c>, <c>context_length_exceeded</c>) is
/// preserved for telemetry.
/// </para>
/// </remarks>
public sealed class ClientModelProviderErrorClassifier : IAIProviderErrorClassifier
{
    /// <inheritdoc />
    public AIProviderErrorInfo? Classify(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is ClientResultException cre)
            {
                return ClassifyClientResultException(cre);
            }
        }

        return null;
    }

    private static AIProviderErrorInfo ClassifyClientResultException(ClientResultException ex)
    {
        // Status == 0 means the request never made it to the server.
        if (ex.Status == 0)
        {
            return new AIProviderErrorInfo(
                AIProviderErrorCategory.NetworkError,
                "Couldn't reach the AI service. Check your connection and try again.",
                ProviderCode: null,
                ex.Message);
        }

        var providerCode = TryExtractProviderCode(ex);
        return ProviderErrorMapping.FromHttpStatus(ex.Status, ex.Message, providerCode);
    }

    /// <summary>
    /// Best-effort extraction of an OpenAI-shaped error code from the raw response body.
    /// Returns null on any failure so the HTTP-status mapping still provides a fallback code.
    /// </summary>
    private static string? TryExtractProviderCode(ClientResultException ex)
    {
        try
        {
            var response = ex.GetRawResponse();
            var content = response?.Content;
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

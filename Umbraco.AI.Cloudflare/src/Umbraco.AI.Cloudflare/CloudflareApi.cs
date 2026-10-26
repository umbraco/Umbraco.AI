using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// Request building and error reporting shared by the extraction client and the connection check, so the
/// URL escaping, auth header and "never put the token in a message" rules live in one place.
/// </summary>
internal static class CloudflareApi
{
    private const string DefaultEndpoint = "https://api.cloudflare.com/client/v4";
    private const int MaxFallbackBodyLength = 500;

    /// <summary>
    /// Throws when a required setting is missing, or the token couldn't be sent as a header value.
    /// </summary>
    public static void ValidateSettings(CloudflareProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AccountId))
        {
            throw new InvalidOperationException("Cloudflare account ID is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiToken))
        {
            throw new InvalidOperationException("Cloudflare API token is required.");
        }

        // Rejected here with a fixed message: a header-format failure further down would quote the value.
        if (settings.ApiToken.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            throw new InvalidOperationException("Cloudflare API token contains invalid characters.");
        }
    }

    /// <summary>
    /// Builds <c>{Endpoint}/accounts/{AccountId}/{path}</c>. The account ID is escaped, so a value such as
    /// <c>a/b</c> can't change which route is called.
    /// </summary>
    public static Uri BuildAccountUri(CloudflareProviderSettings settings, string path)
    {
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint) ? DefaultEndpoint : settings.Endpoint.TrimEnd('/');
        return new Uri($"{endpoint}/accounts/{Uri.EscapeDataString(settings.AccountId!)}/{path}");
    }

    public static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, CloudflareProviderSettings settings, string? json = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
        return request;
    }

    /// <summary>
    /// Builds the exception for a failed response (and disposes it): an <see cref="HttpRequestException"/>
    /// carrying the status code, with Cloudflare's <c>errors[].code</c> and message when the body has them.
    /// The API token is never part of the message.
    /// </summary>
    public static async Task<HttpRequestException> CreateExceptionAsync(
        HttpResponseMessage response,
        CloudflareProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var statusCode = response.StatusCode;
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        finally
        {
            response.Dispose();
        }

        var detail = Describe(TryFormatErrors(body) ?? body, settings.ApiToken);
        return new HttpRequestException(
            $"Cloudflare returned {(int)statusCode} ({statusCode}). {detail}".TrimEnd(),
            inner: null,
            statusCode: statusCode);
    }

    /// <summary>
    /// Formats an <c>errors</c> array (<c>[{ code, message }]</c>) as <c>[code] message</c> pairs, or returns
    /// <see langword="null"/> when <paramref name="body"/> isn't JSON or has no such array.
    /// </summary>
    public static string? TryFormatErrors(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return TryFormatErrors(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? TryFormatErrors(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var error in errors.EnumerateArray())
        {
            if (error.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var code = error.TryGetProperty("code", out var c) ? c.ToString() : null;
            var message = error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            if (code is null && message is null)
            {
                continue;
            }

            parts.Add(code is null ? message! : $"[{code}] {message}".TrimEnd());
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    /// <summary>
    /// Makes Cloudflare-supplied text safe to put in a message: the token is masked first, then the length
    /// is capped, so a token straddling the cap can't leave a readable fragment behind.
    /// </summary>
    public static string Describe(string text, string? token)
    {
        var redacted = string.IsNullOrEmpty(token) ? text : text.Replace(token, "***", StringComparison.Ordinal);
        return redacted.Length <= MaxFallbackBodyLength ? redacted : redacted[..MaxFallbackBodyLength] + "...";
    }
}

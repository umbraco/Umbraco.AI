using System.Text.RegularExpressions;
using Google.GenAI;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Google.Errors;

/// <summary>
/// Maps Google.GenAI SDK errors, which the shared <see cref="ProviderErrorMapping"/> can't read:
/// they derive from <see cref="HttpRequestException"/> but keep the HTTP status in their own
/// <c>StatusCode</c> property, so the inherited <see cref="HttpRequestException.StatusCode"/> is
/// always <c>null</c> and the shared mapping would report every one as a network failure.
/// </summary>
/// <remarks>
/// <para>
/// Invoked by <see cref="GoogleProvider.ClassifyError"/>, which only ever sees exceptions Google
/// produced. Returns <c>null</c> when no Google API error is found, leaving the caller to fall back
/// to the shared mapping (for genuine transport failures such as DNS or TLS).
/// </para>
/// <para>
/// <see cref="ClientError"/> (4xx) and <see cref="ServerError"/> (5xx) are matched directly rather than
/// through their <c>ApiException</c> base, because Google.GenAI only introduced that base class after
/// the lowest version this package supports.
/// </para>
/// </remarks>
internal static partial class GoogleErrorMapping
{
    /// <summary>
    /// Attempts to classify a Google.GenAI API error from its HTTP status and error details.
    /// </summary>
    /// <returns>The classified info, or <c>null</c> if the exception chain holds no Google API error.</returns>
    public static AIProviderErrorInfo? TryClassify(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var (status, googleStatus) = current switch
            {
                ClientError clientError => (clientError.StatusCode, clientError.Status),
                ServerError serverError => (serverError.StatusCode, serverError.Status),
                _ => (0, null),
            };

            // Status 0 means the SDK threw without an HTTP response (e.g. an unreadable body), so
            // there is nothing here to classify by.
            if (status > 0)
            {
                return Classify(status, googleStatus, current.Message);
            }
        }

        return null;
    }

    private static AIProviderErrorInfo Classify(int status, string? googleStatus, string rawMessage)
    {
        // An invalid API key is reported as HTTP 400 INVALID_ARGUMENT, so the status alone would call
        // it a bad request. The error details carry the real reason.
        if (status == 400 && rawMessage.Contains("API_KEY_INVALID", StringComparison.Ordinal))
        {
            return new AIProviderErrorInfo(
                AIProviderErrorCategory.Authentication,
                "Authentication failed. Check the connection's API key.",
                ProviderCode: "API_KEY_INVALID",
                rawMessage);
        }

        var info = ProviderErrorMapping.FromHttpStatus(
            status,
            rawMessage,
            string.IsNullOrEmpty(googleStatus) ? null : googleStatus);

        // The shared rate-limit wording says "wait a moment", which is wrong for a daily quota that
        // will not clear for hours. Google names the exhausted quota in the error details.
        return info.Category == AIProviderErrorCategory.RateLimited && IsDailyQuota(rawMessage)
            ? info with
            {
                UserMessage = "The daily request quota for this model has been reached. "
                              + "Try again later, or use a different model or API key.",
            }
            : info;
    }

    private static bool IsDailyQuota(string rawMessage) => DailyQuotaId().IsMatch(rawMessage);

    // e.g. "quotaId":"GenerateRequestsPerDayPerProjectPerModel-FreeTier"
    [GeneratedRegex("\"quotaId\"\\s*:\\s*\"[^\"]*PerDay", RegexOptions.CultureInvariant)]
    private static partial Regex DailyQuotaId();
}

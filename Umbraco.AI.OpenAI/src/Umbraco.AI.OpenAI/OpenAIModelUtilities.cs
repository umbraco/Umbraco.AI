using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with OpenAI models.
/// </summary>
internal static class OpenAIModelUtilities
{
    /// <summary>
    /// Model families that accept the sampling parameters (<c>temperature</c>, <c>top_p</c>).
    /// </summary>
    /// <remarks>
    /// OpenAI's reasoning models (the <c>o</c>-series and the GPT-5 family) restrict the sampling
    /// parameters — a non-default <c>temperature</c> is rejected rather than ignored. Deliberately an
    /// allow-list rather than a deny-list, so that it fails safe: a stale allow-list degrades a request
    /// by dropping a value the model would have honoured, whereas a stale deny-list fails one outright.
    /// It also only has to be accurate about models that already exist, which is a closed set.
    /// </remarks>
    private static readonly Regex[] SamplingParameterModelPatterns =
    [
        // GPT-3.5, GPT-4, GPT-4o, GPT-4.1 and their variants.
        new(@"^gpt-3\.5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // The ChatGPT-branded snapshots (e.g. chatgpt-4o-latest).
        new(@"^chatgpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Determines whether an OpenAI model accepts the sampling parameters (<c>temperature</c>,
    /// <c>top_p</c>).
    /// </summary>
    /// <param name="modelId">The model ID, or <c>null</c> if no model has been resolved.</param>
    /// <returns>
    /// <c>true</c> when the model is a known family that accepts them; otherwise <c>false</c>. Unknown and
    /// unresolved models — including the <c>o</c>-series and GPT-5 reasoning models — return <c>false</c>
    /// so the parameters are dropped rather than risking a rejected request.
    /// </returns>
    public static bool SupportsSamplingParameters(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && SamplingParameterModelPatterns.Any(p => p.IsMatch(modelId));

    /// <summary>
    /// Formats a model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "gpt-4o-mini", "text-embedding-3-large").</param>
    /// <returns>A formatted display name (e.g., "GPT 4o Mini", "Text Embedding 3 Large").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.Equals("gpt", StringComparison.OrdinalIgnoreCase))
                return "GPT";
            if (part.Equals("o1", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("o3", StringComparison.OrdinalIgnoreCase))
                return part.ToUpperInvariant();
            if (part.All(char.IsDigit))
                return part;
            if (part.Length > 0)
                return char.ToUpperInvariant(part[0]) + part[1..];
            return part;
        });
        return string.Join(" ", formatted);
    }
}

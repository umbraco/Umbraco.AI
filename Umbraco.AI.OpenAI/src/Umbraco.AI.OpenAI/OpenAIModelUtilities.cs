using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with OpenAI models.
/// </summary>
internal static class OpenAIModelUtilities
{
    /// <summary>
    /// Model families that accept a reasoning effort: the o-series and the GPT-5 line, whose current
    /// members use dotted minors (gpt-5.4, gpt-5.5, gpt-5.6 and its sol/terra/luna variants).
    /// </summary>
    /// <remarks>
    /// The o-series is a closed set, not a snapshot: the naming was retired in favour of GPT-5, there is
    /// no o5, and o1/o3 are scheduled for shutdown on 23 October 2026 (o3-deep-research on 11 December
    /// 2026) with gpt-5.6-sol as the replacement. Those entries can be dropped once the shutdowns land;
    /// they are kept for accounts that still have access. Future reasoning models arrive as gpt-5 minors,
    /// which <c>^gpt-5</c> already covers — a further naming change is what would need a new pattern.
    /// </remarks>
    private static readonly Regex[] ReasoningModelPatterns =
    [
        new(@"^o1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o4", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// GPT-5 variants that are not reasoning models despite the family prefix. Matches both the
    /// undotted (<c>gpt-5-chat-latest</c>) and dotted (<c>gpt-5.6-chat</c>) naming.
    /// </summary>
    private static readonly Regex[] NonReasoningExceptionPatterns =
    [
        new(@"^gpt-5[\d.]*-chat", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Whether the model accepts a reasoning effort (<c>reasoning.effort</c> on the Responses API).
    /// </summary>
    /// <param name="modelId">The model ID, or null when unresolved.</param>
    /// <remarks>
    /// A positive list: a reasoning family released after this package ships reads as unsupported until
    /// the list is updated. That only suppresses the setting in the editor and skips sending it — it
    /// never produces a failed request — which is the safe direction for an unknown model.
    /// </remarks>
    public static bool SupportsReasoningEffort(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && ReasoningModelPatterns.Any(p => p.IsMatch(modelId))
           && !NonReasoningExceptionPatterns.Any(p => p.IsMatch(modelId));

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
    /// Embedding models that accept a <c>dimensions</c> request parameter.
    /// </summary>
    /// <remarks>
    /// Shortening an embedding is a <c>text-embedding-3</c> feature; <c>ada-002</c> predates it. Written as
    /// an allow-list so a model this package has not heard of is treated as not supporting it, which drops
    /// the parameter rather than risking a rejected request — the same failure direction the sampling
    /// predicate chooses, and for the same reason.
    /// </remarks>
    private static readonly Regex[] DimensionsModelPatterns =
    [
        new(@"^text-embedding-3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Determines whether an OpenAI embedding model accepts a <c>dimensions</c> parameter.
    /// </summary>
    /// <param name="modelId">The model ID, or <c>null</c> if no model has been resolved.</param>
    public static bool SupportsDimensions(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && DimensionsModelPatterns.Any(p => p.IsMatch(modelId));

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

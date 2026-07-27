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

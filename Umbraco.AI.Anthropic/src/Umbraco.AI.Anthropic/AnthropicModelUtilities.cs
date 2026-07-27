using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Anthropic Claude models.
/// </summary>
internal static class AnthropicModelUtilities
{
    /// <summary>
    /// Model families that reject an explicit extended-thinking token budget with a 400, the same way
    /// they reject the sampling parameters: Opus 4.7, Opus 4.8, Opus 5 and Sonnet 5.
    /// </summary>
    private static readonly Regex[] NoThinkingBudgetPatterns =
    [
        new(@"^claude-opus-4-7", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-opus-4-8", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-opus-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-sonnet-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Whether the model accepts an explicit extended-thinking token budget
    /// (<c>thinking.budget_tokens</c>).
    /// </summary>
    /// <param name="modelId">The model ID, or null when unresolved.</param>
    /// <remarks>
    /// A deny list, so a Claude model released after this package ships is treated as supporting a
    /// budget. That is the right default here: the budget is the long-standing behaviour and only the
    /// newest families restrict it.
    /// </remarks>
    public static bool SupportsThinkingBudget(string? modelId)
        => string.IsNullOrWhiteSpace(modelId)
           || !NoThinkingBudgetPatterns.Any(p => p.IsMatch(modelId));

    /// <summary>
    /// Formats a Claude model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "claude-3-5-sonnet-20241022", "claude-sonnet-4-20250514").</param>
    /// <returns>A formatted display name (e.g., "Claude 3.5 Sonnet", "Claude Sonnet 4").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = new List<string>();

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            // Skip date suffixes (8 digits like 20241022)
            if (part.Length == 8 && part.All(char.IsDigit))
            {
                continue;
            }

            // Handle "claude" -> "Claude"
            if (part.Equals("claude", StringComparison.OrdinalIgnoreCase))
            {
                formatted.Add("Claude");
                continue;
            }

            // Handle versions: combine "3" and "5" into "3.5" when appropriate
            if (part.All(char.IsDigit) && i + 1 < parts.Length && parts[i + 1].All(char.IsDigit) && parts[i + 1].Length == 1)
            {
                formatted.Add($"{part}.{parts[i + 1]}");
                i++; // Skip the next part since we combined it
                continue;
            }

            // Handle standalone versions
            if (part.All(char.IsDigit))
            {
                formatted.Add(part);
                continue;
            }

            // Handle model variants (sonnet, opus, haiku) - capitalize first letter
            if (part.Length > 0)
            {
                formatted.Add(char.ToUpperInvariant(part[0]) + part[1..]);
            }
        }

        return string.Join(" ", formatted);
    }
}

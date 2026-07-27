using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Anthropic Claude models.
/// </summary>
internal static class AnthropicModelUtilities
{
    /// <summary>
    /// The models that accept manual extended thinking (<c>thinking.type: "enabled"</c> with
    /// <c>budget_tokens</c>).
    /// </summary>
    /// <remarks>
    /// A closed set: Claude 4.7 and later reject <c>type: "enabled"</c> with a 400 and use adaptive
    /// thinking with <c>output_config.effort</c> instead, so no future model joins this list. The 4.6
    /// generation still accepts a budget but is deprecated.
    /// </remarks>
    private static readonly Regex[] ExtendedThinkingModelPatterns =
    [
        new(@"^claude-3-7-sonnet", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-(opus|sonnet)-4-\d{8}", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-opus-4-1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-(opus|sonnet|haiku)-4-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-(opus|sonnet)-4-6", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-mythos-preview", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Whether the model accepts an explicit extended-thinking token budget
    /// (<c>thinking.budget_tokens</c>).
    /// </summary>
    /// <param name="modelId">The model ID, or null when unresolved.</param>
    /// <remarks>
    /// An allow-list, because the set that accepts a budget is the closed one: Anthropic's docs state
    /// that Claude 4.7 and later reject <c>thinking.type: "enabled"</c> outright, so every model
    /// released from here on rejects it. A model this package has not heard of therefore reads as not
    /// accepting a budget, which suppresses the setting in the editor and skips sending it rather than
    /// producing a 400.
    /// </remarks>
    public static bool SupportsThinkingBudget(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && ExtendedThinkingModelPatterns.Any(p => p.IsMatch(modelId));

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

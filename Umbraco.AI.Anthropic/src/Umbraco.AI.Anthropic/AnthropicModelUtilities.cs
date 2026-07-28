using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Anthropic Claude models.
/// </summary>
internal static class AnthropicModelUtilities
{
    /// <summary>
    /// Models that do not accept <c>output_config.effort</c>: Claude 3.x, the base Claude 4 models,
    /// Opus 4.1, Sonnet 4.5 and Haiku 4.5.
    /// </summary>
    /// <remarks>
    /// A closed set of legacy models. Effort is supported on Opus 4.5 and everything from the 4.6
    /// generation onwards, so every model released from here on supports it and an unrecognised model is
    /// treated as supporting it.
    /// </remarks>
    private static readonly Regex[] NoEffortPatterns =
    [
        new(@"^claude-3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-(opus|sonnet)-4-\d{8}", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-opus-4-1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-sonnet-4-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-haiku-4-5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Whether the model accepts <c>output_config.effort</c> at all.
    /// </summary>
    /// <param name="modelId">The model ID, or null when unresolved.</param>
    public static bool SupportsEffort(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && !NoEffortPatterns.Any(p => p.IsMatch(modelId));

    /// <summary>
    /// Whether the model accepts the given effort level.
    /// </summary>
    /// <param name="modelId">The model ID, or null when unresolved.</param>
    /// <param name="level">The effort level (case-insensitive).</param>
    /// <remarks>
    /// Only <c>low</c>, <c>medium</c> and <c>high</c> are recognised: those are accepted by every model
    /// that accepts effort at all, so no per-model list is needed. Anthropic's <c>xhigh</c> and <c>max</c>
    /// reach a subset that a hard-coded list cannot track — the set with <c>xhigh</c> grows with each
    /// release — so they are treated as unrecognised and skipped rather than guessed at. Adding them means
    /// reading the models endpoint's per-model <c>capabilities.effort</c>.
    /// </remarks>
    public static bool SupportsEffortLevel(string? modelId, string level)
        => SupportsEffort(modelId)
           && level.Trim().ToLowerInvariant() is "low" or "medium" or "high";

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

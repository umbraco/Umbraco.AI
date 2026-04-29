using System.Globalization;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Perplexity Sonar models.
/// </summary>
internal static class PerplexityModelUtilities
{
    /// <summary>
    /// Formats a Perplexity model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "sonar", "sonar-pro", "sonar-reasoning-pro").</param>
    /// <returns>A formatted display name (e.g., "Sonar", "Sonar Pro", "Sonar Reasoning Pro").</returns>
    public static string FormatDisplayName(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var parts = modelId.Split('-', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", parts.Select(p => textInfo.ToTitleCase(p.ToLowerInvariant())));
    }
}

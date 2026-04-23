namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Mistral models.
/// </summary>
internal static class MistralModelUtilities
{
    /// <summary>
    /// Formats a Mistral model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "mistral-large-latest", "open-mixtral-8x22b", "codestral-2405").</param>
    /// <returns>A formatted display name (e.g., "Mistral Large Latest", "Open Mixtral 8x22B", "Codestral 2405").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            // Keep version numbers (including mixed forms like "8x22b") as-is, but uppercase trailing letters.
            if (char.IsDigit(part[0]))
            {
                formatted.Add(part.ToUpperInvariant());
                continue;
            }

            // Capitalize first letter of words like "mistral", "large", "latest", "sonnet".
            formatted.Add(char.ToUpperInvariant(part[0]) + part[1..]);
        }

        return string.Join(" ", formatted);
    }
}

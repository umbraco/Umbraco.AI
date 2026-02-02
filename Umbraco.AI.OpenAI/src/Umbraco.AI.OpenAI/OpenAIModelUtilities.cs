namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with OpenAI models.
/// </summary>
internal static class OpenAIModelUtilities
{
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

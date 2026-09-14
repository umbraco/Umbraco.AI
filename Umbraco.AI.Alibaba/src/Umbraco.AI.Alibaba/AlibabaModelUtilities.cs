namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Alibaba Cloud (Qwen) models.
/// </summary>
internal static class AlibabaModelUtilities
{
    private static readonly HashSet<string> Acronyms = new(StringComparer.OrdinalIgnoreCase) { "vl" };

    /// <summary>
    /// Formats an Alibaba model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "qwen-plus", "qwen3-max", "qwen-vl-max", "text-embedding-v4").</param>
    /// <returns>A formatted display name (e.g., "Qwen Plus", "Qwen3 Max", "Qwen VL Max", "Text Embedding V4").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.StartsWith("qwen", StringComparison.OrdinalIgnoreCase))
                return "Qwen" + part[4..];

            if (Acronyms.Contains(part))
                return part.ToUpperInvariant();

            // Version tokens like "v4" → "V4"
            if (part.Length >= 2 &&
                (part[0] == 'v' || part[0] == 'V') &&
                part[1..].All(char.IsDigit))
            {
                return "V" + part[1..];
            }

            if (part.All(char.IsDigit))
                return part;

            if (part.Length > 0)
                return char.ToUpperInvariant(part[0]) + part[1..];

            return part;
        });
        return string.Join(" ", formatted);
    }
}

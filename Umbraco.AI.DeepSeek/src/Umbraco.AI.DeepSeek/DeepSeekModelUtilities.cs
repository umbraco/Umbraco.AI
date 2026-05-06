namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with DeepSeek models.
/// </summary>
internal static class DeepSeekModelUtilities
{
    /// <summary>
    /// Formats a DeepSeek model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "deepseek-chat", "deepseek-v4-flash").</param>
    /// <returns>A formatted display name (e.g., "DeepSeek Chat", "DeepSeek V4 Flash").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.Equals("deepseek", StringComparison.OrdinalIgnoreCase))
                return "DeepSeek";

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

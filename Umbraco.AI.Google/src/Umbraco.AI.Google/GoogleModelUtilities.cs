namespace Umbraco.Ai.Extensions;

/// <summary>
/// Utility methods for working with Google AI models.
/// </summary>
internal static class GoogleModelUtilities
{
    /// <summary>
    /// Formats a model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "gemini-2.0-flash", "gemini-1.5-pro").</param>
    /// <returns>A formatted display name (e.g., "Gemini 2.0 Flash", "Gemini 1.5 Pro").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.Equals("gemini", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini";
            }

            if (part.All(char.IsDigit) || part.Contains('.'))
            {
                return part;
            }

            if (part.Length > 0)
            {
                return char.ToUpperInvariant(part[0]) + part[1..];
            }

            return part;
        });
        return string.Join(" ", formatted);
    }
}

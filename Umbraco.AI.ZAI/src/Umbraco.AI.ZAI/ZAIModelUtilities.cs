namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Z.AI's GLM models.
/// </summary>
internal static class ZAIModelUtilities
{
    /// <summary>
    /// Formats a GLM model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "glm-4.5", "glm-5.3-flash", "glm-5-turbo").</param>
    /// <returns>A formatted display name (e.g., "GLM 4.5", "GLM 5.3 Flash", "GLM 5 Turbo").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.Equals("glm", StringComparison.OrdinalIgnoreCase))
                return "GLM";

            if (part.Length > 0)
                return char.ToUpperInvariant(part[0]) + part[1..];

            return part;
        });
        return string.Join(" ", formatted);
    }
}

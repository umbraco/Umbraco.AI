namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Utility methods for working with Microsoft AI Foundry models.
/// </summary>
internal static class MicrosoftFoundryModelUtilities
{
    /// <summary>
    /// Formats a deployment/model ID into a human-readable display name.
    /// </summary>
    /// <param name="deploymentId">The deployment ID (e.g., "gpt-4o", "text-embedding-3-large").</param>
    /// <returns>A formatted display name (e.g., "GPT 4o", "Text Embedding 3 Large").</returns>
    /// <remarks>
    /// Microsoft AI Foundry uses model names that match the underlying model.
    /// This method formats common model naming conventions into human-readable display names.
    /// </remarks>
    public static string FormatDisplayName(string deploymentId)
    {
        var parts = deploymentId.Split('-');
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

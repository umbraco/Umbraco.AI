using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Moonshot (Kimi) models.
/// </summary>
internal static partial class MoonshotModelUtilities
{
    [GeneratedRegex(@"^k\d+(\.\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionSegmentRegex();

    /// <summary>
    /// Formats a Moonshot model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "kimi-k3", "kimi-k2.7-code-highspeed").</param>
    /// <returns>A formatted display name (e.g., "Kimi K3", "Kimi K2.7 Code Highspeed").</returns>
    public static string FormatDisplayName(string modelId)
    {
        var parts = modelId.Split('-');
        var formatted = parts.Select(part =>
        {
            if (part.Equals("kimi", StringComparison.OrdinalIgnoreCase))
                return "Kimi";

            // Version tokens like "k3" or "k2.7" -> "K3" / "K2.7"
            if (VersionSegmentRegex().IsMatch(part))
                return "K" + part[1..];

            if (part.Length > 0)
                return char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();

            return part;
        });
        return string.Join(" ", formatted);
    }
}

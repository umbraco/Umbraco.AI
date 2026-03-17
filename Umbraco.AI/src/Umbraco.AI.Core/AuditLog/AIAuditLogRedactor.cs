using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Shared utility for applying config-based redaction patterns to audit log content.
/// </summary>
internal static class AIAuditLogRedactor
{
    /// <summary>
    /// Applies configured redaction patterns to the input string.
    /// </summary>
    /// <param name="input">The string to redact.</param>
    /// <param name="patterns">The regex patterns to match for redaction.</param>
    /// <param name="logger">Logger for reporting pattern failures.</param>
    /// <returns>The redacted string, or the original if no patterns match.</returns>
    public static string? ApplyRedaction(string? input, IReadOnlyList<string> patterns, ILogger logger)
    {
        if (string.IsNullOrEmpty(input) || patterns.Count == 0)
            return input;

        var result = input;
        foreach (var pattern in patterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to apply redaction pattern: {Pattern}", pattern);
            }
        }
        return result;
    }
}

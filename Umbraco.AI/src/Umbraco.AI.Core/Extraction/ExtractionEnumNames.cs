namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Parses enum member names from caller-supplied strings, shared by the Management API's extract
/// controller and the Automate extract action so both accept exactly the same spellings.
/// </summary>
internal static class ExtractionEnumNames
{
    /// <summary>
    /// Matches <paramref name="value"/> (trimmed, case-insensitive) against the exact member names of
    /// <typeparamref name="TEnum"/>. Unlike <c>Enum.TryParse</c> it rejects numeric strings ("3") and
    /// comma-joined flags ("seconds,days").
    /// </summary>
    /// <remarks>Used by Umbraco.AI.Automate via InternalsVisibleTo, so renaming or changing it breaks an installed Automate at runtime.</remarks>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The matching member, or <see langword="default"/> when there is none.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> names a member.</returns>
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        var trimmed = value?.Trim();
        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                result = Enum.Parse<TEnum>(name);
                return true;
            }
        }

        result = default;
        return false;
    }
}

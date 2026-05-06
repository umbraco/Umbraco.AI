using System.Globalization;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Fireworks AI model ids.
/// </summary>
internal static class FireworksAIModelUtilities
{
    /// <summary>
    /// Formats a Fireworks model id into a human-readable display name.
    /// </summary>
    /// <example>
    /// <c>accounts/fireworks/models/llama-v3p3-70b-instruct</c> -&gt; <c>Llama 3.3 70B Instruct</c>.
    /// <c>accounts/fireworks/models/qwen3-embedding-8b</c> -&gt; <c>Qwen3 Embedding 8B</c>.
    /// </example>
    public static string FormatDisplayName(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        // Strip the accounts/{acct}/models/ prefix if present.
        var bareName = modelId;
        var lastSlash = modelId.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < modelId.Length - 1)
        {
            bareName = modelId[(lastSlash + 1)..];
        }

        var parts = bareName.Split('-');
        var formatted = new List<string>(parts.Length);

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
            {
                continue;
            }

            // Version tokens like "v3p1" or "v3p3" -> "3.1", "3.3"
            if (TryFormatVersionToken(part, out var version))
            {
                formatted.Add(version);
                continue;
            }

            // Parameter-size tokens like "70b", "8b", "480b" -> "70B"
            if (TryFormatParameterSize(part, out var size))
            {
                formatted.Add(size);
                continue;
            }

            // Plain integers stay as-is
            if (part.All(char.IsDigit))
            {
                formatted.Add(part);
                continue;
            }

            // Word: capitalise first character, keep any trailing digits.
            // Handles "qwen3" -> "Qwen3", "llama" -> "Llama".
            formatted.Add(Capitalise(part));
        }

        return string.Join(' ', formatted);
    }

    /// <summary>
    /// Parses a Fireworks version token like <c>v3p1</c> or <c>v2p5</c> into <c>3.1</c> / <c>2.5</c>.
    /// </summary>
    private static bool TryFormatVersionToken(string token, out string version)
    {
        version = string.Empty;

        if (token.Length < 4 || (token[0] != 'v' && token[0] != 'V'))
        {
            return false;
        }

        var pIdx = token.IndexOf('p', 1);
        if (pIdx <= 1 || pIdx >= token.Length - 1)
        {
            return false;
        }

        var major = token[1..pIdx];
        var minor = token[(pIdx + 1)..];

        if (!major.All(char.IsDigit) || !minor.All(char.IsDigit))
        {
            return false;
        }

        version = string.Create(CultureInfo.InvariantCulture, $"{major}.{minor}");
        return true;
    }

    /// <summary>
    /// Parses a parameter-size token like <c>70b</c>, <c>8b</c>, <c>480b</c>.
    /// </summary>
    private static bool TryFormatParameterSize(string token, out string size)
    {
        size = string.Empty;

        if (token.Length < 2)
        {
            return false;
        }

        var last = token[^1];
        if (last != 'b' && last != 'B' && last != 'm' && last != 'M')
        {
            return false;
        }

        var digits = token[..^1];
        if (digits.Length == 0 || !digits.All(char.IsDigit))
        {
            return false;
        }

        size = digits + char.ToUpperInvariant(last);
        return true;
    }

    private static string Capitalise(string word)
        => word.Length switch
        {
            0 => word,
            1 => word.ToUpperInvariant(),
            _ => char.ToUpperInvariant(word[0]) + word[1..],
        };
}

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with OpenRouter model ids.
/// </summary>
internal static class OpenRouterModelUtilities
{
    /// <summary>
    /// Formats an OpenRouter model into a human-readable display name.
    /// </summary>
    /// <remarks>
    /// OpenRouter's models endpoint already returns a vendor-authored display name (e.g.
    /// <c>Anthropic: Claude v2.0</c>) for every model in its catalog, so that is preferred whenever
    /// present. The fallback only applies to a model id typed manually or otherwise missing from the
    /// last listing, e.g. <c>openai/gpt-4o</c> -&gt; <c>Openai / Gpt 4o</c>.
    /// </remarks>
    public static string FormatDisplayName(string modelId, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        return string.Join(" / ", modelId.Split('/').Select(FormatSegment));
    }

    private static string FormatSegment(string segment)
        => string.Join(' ', segment.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries).Select(Capitalise));

    private static string Capitalise(string word)
        => word.Length switch
        {
            0 => word,
            1 => word.ToUpperInvariant(),
            _ => char.ToUpperInvariant(word[0]) + word[1..],
        };
}

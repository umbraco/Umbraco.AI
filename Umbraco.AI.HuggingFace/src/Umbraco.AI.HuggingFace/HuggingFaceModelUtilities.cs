namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Hugging Face model identifiers.
/// </summary>
internal static class HuggingFaceModelUtilities
{
    /// <summary>
    /// Formats a Hugging Face model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">
    /// The model ID, typically "vendor/model-name" with an optional ":provider" or
    /// ":fastest|cheapest|preferred" routing suffix
    /// (e.g., "meta-llama/Meta-Llama-3.1-70B-Instruct", "openai/gpt-oss-120b:fastest").
    /// </param>
    /// <returns>A formatted display name (e.g., "meta-llama / Meta Llama 3.1 70B Instruct (fastest)").</returns>
    public static string FormatDisplayName(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        var routingSuffix = string.Empty;
        var withoutSuffix = modelId;
        var colonIndex = modelId.IndexOf(':');
        if (colonIndex > 0 && colonIndex < modelId.Length - 1)
        {
            routingSuffix = modelId[(colonIndex + 1)..];
            withoutSuffix = modelId[..colonIndex];
        }

        var slashIndex = withoutSuffix.IndexOf('/');
        var vendor = slashIndex > 0 ? withoutSuffix[..slashIndex] : null;
        var name = slashIndex > 0 ? withoutSuffix[(slashIndex + 1)..] : withoutSuffix;

        var formattedName = FormatModelName(name);
        var displayName = vendor is null ? formattedName : $"{vendor} / {formattedName}";

        return string.IsNullOrEmpty(routingSuffix) ? displayName : $"{displayName} ({routingSuffix})";
    }

    private static string FormatModelName(string name)
    {
        var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0 || part.Any(char.IsDigit))
            {
                continue;
            }

            parts[i] = char.ToUpperInvariant(part[0]) + part[1..];
        }

        return string.Join(' ', parts);
    }
}

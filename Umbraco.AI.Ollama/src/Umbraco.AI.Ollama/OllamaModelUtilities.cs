namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Ollama models.
/// </summary>
internal static class OllamaModelUtilities
{
    /// <summary>
    /// Formats an Ollama model ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "llama3.2:latest", "mistral:7b", "codellama:13b").</param>
    /// <returns>A formatted display name (e.g., "Llama 3.2", "Mistral 7B", "CodeLlama 13B").</returns>
    public static string FormatDisplayName(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        // Split by colon to separate model name from tag/variant
        var parts = modelId.Split(':', 2);
        var modelName = parts[0];
        var tag = parts.Length > 1 ? parts[1] : null;

        // Format the model name
        var formattedName = FormatModelName(modelName);

        // Append tag if it's not "latest" (most common default)
        if (!string.IsNullOrWhiteSpace(tag) && !tag.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            formattedName += $" ({FormatTag(tag)})";
        }

        return formattedName;
    }

    private static string FormatModelName(string modelName)
    {
        // Handle common model patterns
        // Examples: "llama3.2" -> "Llama 3.2", "codellama" -> "CodeLlama", "mistral" -> "Mistral"

        var result = modelName;

        // Capitalize first letter
        if (result.Length > 0)
        {
            result = char.ToUpperInvariant(result[0]) + result[1..];
        }

        // Handle version numbers with dots (e.g., "llama3.2" -> "Llama 3.2")
        if (result.Contains('.'))
        {
            var lastLetterIndex = -1;
            for (var i = 0; i < result.Length; i++)
            {
                if (char.IsLetter(result[i]))
                {
                    lastLetterIndex = i;
                }
                else if (char.IsDigit(result[i]) && lastLetterIndex >= 0)
                {
                    // Insert space between last letter and first digit
                    result = result[..(lastLetterIndex + 1)] + " " + result[(lastLetterIndex + 1)..];
                    break;
                }
            }
        }

        // Handle camelCase model names (e.g., "codeLlama" -> "Code Llama")
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            "([a-z])([A-Z])",
            "$1 $2"
        );

        return result;
    }

    private static string FormatTag(string tag)
    {
        // Format common tags
        // Examples: "7b" -> "7B", "13b" -> "13B", "instruct" -> "Instruct"

        if (string.IsNullOrWhiteSpace(tag))
        {
            return tag;
        }

        // Handle parameter size tags (e.g., "7b", "13b", "70b")
        if (tag.EndsWith("b", StringComparison.OrdinalIgnoreCase) &&
            tag.Length > 1 &&
            tag[..^1].All(char.IsDigit))
        {
            return tag[..^1] + "B";
        }

        // Capitalize first letter for other tags
        return char.ToUpperInvariant(tag[0]) + tag[1..].ToLowerInvariant();
    }
}

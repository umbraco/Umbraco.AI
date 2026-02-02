namespace Umbraco.Ai.Extensions;

/// <summary>
/// Utility methods for working with Amazon Bedrock models.
/// </summary>
internal static class AmazonModelUtilities
{
    /// <summary>
    /// Formats a Bedrock model ID or inference profile ID into a human-readable display name.
    /// </summary>
    /// <param name="modelId">The model ID (e.g., "eu.amazon.nova-lite-v1:0", "amazon.nova-lite-v1:0").</param>
    /// <returns>A formatted display name (e.g., "Amazon Nova Lite V1 (EU)", "Anthropic Claude 3 Sonnet").</returns>
    public static string FormatDisplayName(string modelId)
    {
        // Remove version suffix (e.g., ":0")
        var versionIndex = modelId.IndexOf(':');
        var baseModelId = versionIndex >= 0 ? modelId[..versionIndex] : modelId;

        // Check for region prefix (e.g., "eu.", "us.", "apac.")
        string? regionSuffix = null;
        if (baseModelId.StartsWith("eu.", StringComparison.OrdinalIgnoreCase))
        {
            regionSuffix = "(EU)";
            baseModelId = baseModelId[3..];
        }
        else if (baseModelId.StartsWith("us.", StringComparison.OrdinalIgnoreCase))
        {
            regionSuffix = "(US)";
            baseModelId = baseModelId[3..];
        }
        else if (baseModelId.StartsWith("apac.", StringComparison.OrdinalIgnoreCase))
        {
            regionSuffix = "(APAC)";
            baseModelId = baseModelId[5..];
        }

        // Split by '.' to separate provider from model name
        var dotParts = baseModelId.Split('.');
        if (dotParts.Length < 2)
        {
            var name = FormatPart(baseModelId);
            return regionSuffix is not null ? $"{name} {regionSuffix}" : name;
        }

        var provider = FormatProviderName(dotParts[0]);
        var modelName = FormatModelName(string.Join(".", dotParts[1..]));

        var displayName = $"{provider} {modelName}";
        return regionSuffix is not null ? $"{displayName} {regionSuffix}" : displayName;
    }

    private static string FormatProviderName(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "amazon" => "Amazon",
            "anthropic" => "Anthropic",
            "meta" => "Meta",
            "mistral" => "Mistral",
            "cohere" => "Cohere",
            "ai21" => "AI21",
            "stability" => "Stability",
            _ => FormatPart(provider)
        };
    }

    private static string FormatModelName(string modelName)
    {
        var parts = modelName.Split('-');
        var formatted = new List<string>();

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            // Skip date suffixes (8 digits like 20240229)
            if (part.Length == 8 && part.All(char.IsDigit))
            {
                continue;
            }

            // Skip version suffixes (e.g., "v1", "v2")
            if (part.StartsWith('v') && part.Length <= 3 && part[1..].All(char.IsDigit))
            {
                // Include version
                formatted.Add(part.ToUpperInvariant());
                continue;
            }

            // Handle versions: combine "3" and "5" into "3.5" when appropriate
            if (part.All(char.IsDigit) && i + 1 < parts.Length && parts[i + 1].All(char.IsDigit) && parts[i + 1].Length == 1)
            {
                formatted.Add($"{part}.{parts[i + 1]}");
                i++; // Skip the next part since we combined it
                continue;
            }

            // Handle standalone versions
            if (part.All(char.IsDigit))
            {
                formatted.Add(part);
                continue;
            }

            formatted.Add(FormatPart(part));
        }

        return string.Join(" ", formatted);
    }

    private static string FormatPart(string part)
    {
        if (string.IsNullOrEmpty(part))
        {
            return part;
        }

        // Handle known abbreviations
        var upper = part.ToUpperInvariant();
        if (upper is "V1" or "V2" or "V3" or "PRO" or "LITE" or "MICRO" or "PREMIER")
        {
            return char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
        }

        // Capitalize first letter
        return char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
    }
}

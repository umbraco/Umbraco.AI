using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Amazon Bedrock models.
/// </summary>
internal static class AmazonModelUtilities
{
    /// <summary>
    /// Strips the optional region prefix (<c>eu.</c>, <c>us.</c>, <c>apac.</c>) that inference profile IDs
    /// carry, and the trailing Bedrock version suffix (<c>-v1:0</c>).
    /// </summary>
    private static readonly Regex BedrockIdDecorations =
        new(@"^(eu|us|apac)\.|-v\d+:\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Vendor-qualified model families that accept the sampling parameters (<c>temperature</c>,
    /// <c>top_p</c>, <c>top_k</c>), matched against the region- and version-stripped model ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bedrock fronts other vendors' models, so support is inherited from whoever built the model rather
    /// than being an Amazon property: a Bedrock-hosted <c>anthropic.claude-opus-4-8</c> rejects the
    /// sampling parameters for exactly the same reason the first-party Anthropic model does. The vendor
    /// and family are both present in the ID, so they can be matched directly.
    /// </para>
    /// <para>
    /// Because provider packages cannot reference each other, the Claude rules below are a deliberate
    /// duplicate of the ones in <c>Umbraco.AI.Anthropic</c>. Keeping a local copy is preferable to a shared
    /// table in core, which would couple core releases to vendor model launches. If the two copies drift,
    /// the worst case is a dropped temperature on a Bedrock-hosted Claude that would have accepted it.
    /// </para>
    /// <para>
    /// As with the other providers this is an <em>allow</em>-list, so anything unrecognised — a vendor we
    /// don't enumerate, or a family newer than this list — fails safe by dropping the parameters.
    /// </para>
    /// </remarks>
    private static readonly Regex[] SamplingParameterModelPatterns =
    [
        // Amazon's own models.
        new(@"^amazon\.nova-", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Mistral and Meta Llama accept the sampling parameters across their Bedrock catalogue.
        new(@"^mistral\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^meta\.llama", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Claude — mirrors Umbraco.AI.Anthropic. Claude 3, 3.5 and 3.7.
        new(@"^anthropic\.claude-3(-|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Claude 4 with no minor version (the trailing 8-digit group is a release date).
        new(@"^anthropic\.claude-(opus|sonnet|haiku)-4(-\d{8})?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Claude 4.0 / 4.1 / 4.5 / 4.6. 4.7 and 4.8 are deliberately excluded.
        new(@"^anthropic\.claude-(opus|sonnet|haiku)-4-[0156](-\d{8})?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Determines whether a Bedrock model accepts the sampling parameters (<c>temperature</c>,
    /// <c>top_p</c>, <c>top_k</c>).
    /// </summary>
    /// <param name="modelId">
    /// The Bedrock model or inference profile ID, with or without a region prefix and version suffix
    /// (e.g. <c>us.anthropic.claude-opus-4-8-v1:0</c>).
    /// </param>
    /// <returns>
    /// <c>true</c> when the model is a known family that accepts them; otherwise <c>false</c>. Unknown and
    /// unresolved models return <c>false</c> so the parameters are dropped rather than risking a rejected
    /// request — see the remarks on <see cref="SamplingParameterModelPatterns"/>.
    /// </returns>
    public static bool SupportsSamplingParameters(string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var normalised = BedrockIdDecorations.Replace(modelId, string.Empty);

        return SamplingParameterModelPatterns.Any(p => p.IsMatch(normalised));
    }

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

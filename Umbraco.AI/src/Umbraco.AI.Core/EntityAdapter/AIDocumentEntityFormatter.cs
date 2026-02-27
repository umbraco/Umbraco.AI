using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Formatter for Umbraco CMS document/media entities.
/// Provides property-based formatting when the data structure matches the expected CMS format.
/// Falls back to generic JSON formatting if the structure doesn't match.
/// </summary>
internal sealed class AIDocumentEntityFormatter : IAIEntityFormatter
{
    private readonly AIGenericEntityFormatter _genericFormatter = new();

    /// <inheritdoc />
    public string? EntityType => "document";

    /// <inheritdoc />
    public string Format(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Try to extract CMS-specific structure from data
        if (!TryExtractCmsStructure(entity.Data, out var contentType, out var properties))
        {
            // Structure doesn't match - fall back to generic formatter
            return _genericFormatter.Format(entity);
        }

        var sb = new StringBuilder();

        sb.AppendLine($"## Current Entity Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        sb.AppendLine($"Name: `{entity.Name}`");
        sb.AppendLine($"Type: `{entity.EntityType}`");
        sb.AppendLine($"**IMPORTANT** When the user says 'this page', 'this document', 'this entity', 'this media item' or similar, you should use this context entry as the reference.");

        if (!string.IsNullOrEmpty(contentType))
        {
            sb.AppendLine($"Content type: {contentType}");
        }

        if (properties.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Properties");
            sb.AppendLine();

            foreach (var property in properties)
            {
                var valueDisplay = property.Value?.ToString() ?? "(empty)";
                sb.AppendLine($"- **{property.Label}** (`{property.Alias}`): {valueDisplay}");
            }
        }

        return sb.ToString();
    }

    private static bool TryExtractCmsStructure(
        JsonElement data,
        out string? contentType,
        out List<PropertyInfo> properties)
    {
        contentType = null;
        properties = [];

        // Check if data is an object
        if (data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Try to get contentType (optional)
        if (data.TryGetProperty("contentType", out var contentTypeElement)
            && contentTypeElement.ValueKind == JsonValueKind.String)
        {
            contentType = contentTypeElement.GetString();
        }

        // Try to get properties array (required for CMS structure)
        if (!data.TryGetProperty("properties", out var propertiesElement)
            || propertiesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        // Parse properties array
        foreach (var propElement in propertiesElement.EnumerateArray())
        {
            if (propElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // Require alias and label
            if (!propElement.TryGetProperty("alias", out var aliasElement)
                || aliasElement.ValueKind != JsonValueKind.String
                || !propElement.TryGetProperty("label", out var labelElement)
                || labelElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var alias = aliasElement.GetString();
            var label = labelElement.GetString();

            if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(label))
            {
                continue;
            }

            // Value is optional
            object? value = null;
            if (propElement.TryGetProperty("value", out var valueElement))
            {
                value = ExtractValue(valueElement);
            }

            properties.Add(new PropertyInfo(alias, label, value));
        }

        // Consider it a CMS structure if we found at least some properties
        return properties.Count > 0;
    }

    private static object? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(), // For objects/arrays, return JSON string
        };
    }

    private sealed record PropertyInfo(string Alias, string Label, object? Value);
}

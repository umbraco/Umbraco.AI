using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Shared formatting logic for CMS entities (documents, media, members)
/// that use the standard { contentType, properties[] } data structure.
/// </summary>
internal static class CmsEntityFormatHelper
{
    /// <summary>
    /// Formats a CMS entity with property-based structure.
    /// Falls back to generic JSON formatting if the structure doesn't match.
    /// </summary>
    public static string FormatCmsEntity(AISerializedEntity entity)
    {
        if (!TryExtractCmsStructure(entity.Data, out var contentType, out var properties))
        {
            return GenericEntityAdapter.FormatGeneric(entity);
        }

        var sb = new StringBuilder();

        sb.AppendLine("## Current Entity Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        sb.AppendLine($"Name: `{entity.Name}`");
        sb.AppendLine($"Type: `{entity.EntityType}`");
        sb.AppendLine("**IMPORTANT** When the user says 'this page', 'this document', 'this entity', 'this media item' or similar, you should use this context entry as the reference.");

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

        if (data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (data.TryGetProperty("contentType", out var contentTypeElement)
            && contentTypeElement.ValueKind == JsonValueKind.String)
        {
            contentType = contentTypeElement.GetString();
        }

        if (!data.TryGetProperty("properties", out var propertiesElement)
            || propertiesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var propElement in propertiesElement.EnumerateArray())
        {
            if (propElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

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

            object? value = null;
            if (propElement.TryGetProperty("value", out var valueElement))
            {
                value = ExtractValue(valueElement);
            }

            properties.Add(new PropertyInfo(alias, label, value));
        }

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
            _ => element.GetRawText(),
        };
    }

    private sealed record PropertyInfo(string Alias, string Label, object? Value);
}

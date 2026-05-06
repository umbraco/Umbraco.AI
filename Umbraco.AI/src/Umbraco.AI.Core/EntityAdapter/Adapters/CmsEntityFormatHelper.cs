using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

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
    /// When <paramref name="typeCache"/> and <paramref name="schemaService"/> are supplied
    /// (and the entity has a known content type), the rendered prompt embeds the JSON Schema
    /// for each property's input value alongside its current value — so the LLM does not have
    /// to call get_content_type_schema before writing complex editors (block list, block grid,
    /// media picker, etc.).
    /// </summary>
    public static string FormatCmsEntity(
        AISerializedEntity entity,
        IPublishedContentTypeCache? typeCache = null,
        IPropertyEditorSchemaService? schemaService = null,
        PublishedItemType primaryItemType = PublishedItemType.Content)
    {
        if (!TryExtractCmsStructure(entity.Data, out var contentType, out var properties))
        {
            return GenericEntityAdapter.FormatGeneric(entity);
        }

        var schemas = ResolveSchemas(contentType, properties, typeCache, schemaService, primaryItemType);

        var sb = new StringBuilder();

        sb.AppendLine("## Entity Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        if (!string.IsNullOrEmpty(entity.Name))
        {
            sb.AppendLine($"Name: `{entity.Name}`");
        }
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
            if (schemas.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Each property below lists its current value AND the JSON Schema describing the value shape it accepts on write. Use the schema as the source of truth when calling set_value — the rendered current value is for reading only and may not reflect the input shape.");
            }
            sb.AppendLine();

            foreach (var property in properties)
            {
                AppendProperty(sb, property, schemas);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a CMS element (e.g., a block within a document) with property-based structure.
    /// Falls back to generic JSON formatting if the structure doesn't match.
    /// </summary>
    public static string FormatCmsElement(
        AISerializedEntity entity,
        IPublishedContentTypeCache? typeCache = null,
        IPropertyEditorSchemaService? schemaService = null)
    {
        if (!TryExtractCmsStructure(entity.Data, out var contentType, out var properties))
        {
            return GenericEntityAdapter.FormatGeneric(entity);
        }

        var schemas = ResolveSchemas(contentType, properties, typeCache, schemaService, PublishedItemType.Element);

        var sb = new StringBuilder();

        sb.AppendLine("## Current Element Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        if (!string.IsNullOrEmpty(entity.Name))
        {
            sb.AppendLine($"Name: `{entity.Name}`");
        }
        sb.AppendLine($"Type: `{entity.EntityType}`");
        sb.AppendLine("**IMPORTANT** When the user says 'this block', 'this element' or similar, you should use this context entry as the reference. This is the element currently being edited within the parent entity.");

        if (!string.IsNullOrEmpty(contentType))
        {
            sb.AppendLine($"Content type: {contentType}");
        }

        if (properties.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Properties");
            if (schemas.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Each property below lists its current value AND the JSON Schema describing the value shape it accepts on write. Use the schema as the source of truth when calling set_value — the rendered current value is for reading only and may not reflect the input shape.");
            }
            sb.AppendLine();

            foreach (var property in properties)
            {
                AppendProperty(sb, property, schemas);
            }
        }

        return sb.ToString();
    }

    private static void AppendProperty(
        StringBuilder sb,
        PropertyInfo property,
        IReadOnlyDictionary<string, PropertySchemaInfo> schemas)
    {
        var valueDisplay = property.Value?.ToString() ?? "(empty)";

        if (!schemas.TryGetValue(property.Alias, out var schemaInfo))
        {
            sb.AppendLine($"- **{property.Label}** (`{property.Alias}`): {valueDisplay}");
            return;
        }

        sb.AppendLine($"- **{property.Label}** (`{property.Alias}`)");
        if (!string.IsNullOrEmpty(schemaInfo.EditorAlias))
        {
            sb.AppendLine($"    - editor: `{schemaInfo.EditorAlias}`");
        }
        sb.AppendLine($"    - current value: {valueDisplay}");
        if (schemaInfo.Schema is not null)
        {
            sb.AppendLine($"    - input shape (JSON Schema): {schemaInfo.Schema.ToJsonString()}");
        }
    }

    private static IReadOnlyDictionary<string, PropertySchemaInfo> ResolveSchemas(
        string? contentTypeKey,
        IReadOnlyList<PropertyInfo> properties,
        IPublishedContentTypeCache? typeCache,
        IPropertyEditorSchemaService? schemaService,
        PublishedItemType primaryItemType)
    {
        if (typeCache is null
            || schemaService is null
            || string.IsNullOrEmpty(contentTypeKey)
            || !Guid.TryParse(contentTypeKey, out var key))
        {
            return new Dictionary<string, PropertySchemaInfo>(0);
        }

        IPublishedContentType? publishedContentType = TryGetPublishedContentType(typeCache, primaryItemType, key);
        if (publishedContentType is null)
        {
            return new Dictionary<string, PropertySchemaInfo>(0);
        }

        var byAlias = publishedContentType.PropertyTypes
            .ToDictionary(pt => pt.Alias, pt => pt, StringComparer.OrdinalIgnoreCase);

        var schemas = new Dictionary<string, PropertySchemaInfo>(properties.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
        {
            if (!byAlias.TryGetValue(property.Alias, out var pt))
            {
                continue;
            }

            JsonObject? schema;
            try
            {
                schema = schemaService.GetValueSchema(pt.DataType.EditorAlias, pt.DataType.ConfigurationObject);
            }
            catch
            {
                schema = null;
            }

            // Enrich block list / block grid schemas so the LLM sees element-type
            // aliases and per-element-type property schemas next to the bare GUID
            // enum the CMS emits.
            var enriched = Tools.Umbraco.BlockSchemaEnricher.Enrich(schema, typeCache, schemaService);

            schemas[property.Alias] = new PropertySchemaInfo(pt.DataType.EditorAlias, enriched);
        }

        return schemas;
    }

    private static IPublishedContentType? TryGetPublishedContentType(
        IPublishedContentTypeCache cache,
        PublishedItemType primary,
        Guid key)
    {
        // The same Guid may resolve to different PublishedItemType buckets across
        // documents, elements, media and members. Try the primary bucket the
        // adapter was registered for first, then fall back so element-typed
        // payloads still surface schemas.
        foreach (var itemType in PreferredItemTypeOrder(primary))
        {
            try
            {
                var ct = cache.Get(itemType, key);
                if (ct is not null)
                {
                    return ct;
                }
            }
            catch
            {
                // Cache.Get throws when the key isn't registered for that item
                // type. Swallow and try the next bucket.
            }
        }

        return null;
    }

    private static IEnumerable<PublishedItemType> PreferredItemTypeOrder(PublishedItemType primary)
    {
        yield return primary;
        if (primary != PublishedItemType.Content) yield return PublishedItemType.Content;
        if (primary != PublishedItemType.Element) yield return PublishedItemType.Element;
        if (primary != PublishedItemType.Media) yield return PublishedItemType.Media;
        if (primary != PublishedItemType.Member) yield return PublishedItemType.Member;
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

    private sealed record PropertySchemaInfo(string EditorAlias, JsonObject? Schema);
}

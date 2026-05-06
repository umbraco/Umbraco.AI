using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Walks a JSON Schema produced by <c>IPropertyEditorSchemaService</c> and adds
/// an <c>x-allowedElementTypes</c> sibling annotation next to every
/// <c>contentTypeKey.enum</c>. The annotation maps each allowed element-type GUID
/// to its alias and the inline JSON Schema for each of its properties, so an LLM
/// reading the schema knows the human-readable identifier of each block and the
/// shape every block-property value must take.
/// </summary>
/// <remarks>
/// The CMS 17.4.0 schema feature emits element-type GUIDs only in <c>contentTypeKey.enum</c>
/// and types <c>values[].value</c> as <c>{}</c> (any). On its own that is not enough
/// for an LLM to produce a correctly-shaped block list / block grid value: the model
/// has no source of truth for which block aliases the GUIDs map to, nor what shape
/// each block's properties accept. This enricher closes that gap by resolving each
/// GUID through <see cref="IPublishedContentTypeCache"/> and recursively producing
/// the property schemas via <see cref="IPropertyEditorSchemaService"/>.
/// </remarks>
internal static class BlockSchemaEnricher
{
    /// <summary>
    /// Returns an enriched copy of the supplied schema. The original is left untouched.
    /// </summary>
    /// <param name="schema">The bare schema returned by the CMS schema service.</param>
    /// <param name="typeCache">Published-content-type cache used to resolve element type GUIDs.</param>
    /// <param name="schemaService">Schema service used to produce per-property schemas for each element type.</param>
    /// <param name="elementTypeDepth">
    /// Maximum number of element-type expansions to perform. Each block list / block grid
    /// inside an element type's property schema counts as a new level. Default is 1, which
    /// expands the immediately-allowed element types but leaves nested blocks as bare GUID
    /// enums (the LLM can call <c>get_property_value_schema</c> to drill deeper).
    /// </param>
    public static JsonObject? Enrich(
        JsonObject? schema,
        IPublishedContentTypeCache typeCache,
        IPropertyEditorSchemaService schemaService,
        int elementTypeDepth = 1)
    {
        if (schema is null)
        {
            return null;
        }

        // Deep clone via a serialise-roundtrip so the cache's underlying schema instance
        // is never mutated. JsonObject.DeepClone exists in .NET 9+, but a roundtrip is
        // simple, robust, and the schemas are small enough that the overhead is irrelevant.
        var clone = JsonNode.Parse(schema.ToJsonString())!.AsObject();
        EnrichInPlace(clone, typeCache, schemaService, elementTypeDepth);
        return clone;
    }

    private static void EnrichInPlace(
        JsonNode? node,
        IPublishedContentTypeCache typeCache,
        IPropertyEditorSchemaService schemaService,
        int elementTypeDepth)
    {
        switch (node)
        {
            case JsonObject obj:
                // Snapshot the keys before iterating; we may attach a sibling property
                // mid-walk and don't want to perturb the enumerator.
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    var child = obj[key];
                    if (child is null)
                    {
                        continue;
                    }

                    // Block schemas describe element-type allow-lists by attaching an
                    // `enum` of GUIDs to the `contentTypeKey` property of each
                    // contentData / settingsData item. That is the only shape we
                    // enrich — every other node is just walked recursively.
                    if (key == "contentTypeKey"
                        && child is JsonObject contentTypeKeyObj
                        && contentTypeKeyObj["enum"] is JsonArray enumArr
                        && enumArr.Count > 0)
                    {
                        AttachAllowedElementTypes(contentTypeKeyObj, enumArr, typeCache, schemaService, elementTypeDepth);
                    }
                    else
                    {
                        EnrichInPlace(child, typeCache, schemaService, elementTypeDepth);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    EnrichInPlace(item, typeCache, schemaService, elementTypeDepth);
                }
                break;
        }
    }

    private static void AttachAllowedElementTypes(
        JsonObject contentTypeKeyObj,
        JsonArray enumArr,
        IPublishedContentTypeCache typeCache,
        IPropertyEditorSchemaService schemaService,
        int elementTypeDepth)
    {
        var allowed = new JsonArray();

        foreach (var entry in enumArr)
        {
            if (entry is null)
            {
                continue;
            }

            string? raw;
            try
            {
                raw = entry.GetValue<string>();
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var elementTypeKey))
            {
                continue;
            }

            var elementType = TryGetElementType(typeCache, elementTypeKey);
            if (elementType is null)
            {
                continue;
            }

            allowed.Add(BuildElementTypeInfo(elementType, typeCache, schemaService, elementTypeDepth));
        }

        if (allowed.Count > 0)
        {
            contentTypeKeyObj["x-allowedElementTypes"] = allowed;
        }
    }

    private static IPublishedContentType? TryGetElementType(IPublishedContentTypeCache typeCache, Guid key)
    {
        // Block configurations reference element types, but legacy or shared content
        // types may live under the Content bucket too. Try both with try/catch — the
        // cache throws when an item type isn't registered for a given key.
        foreach (var itemType in new[] { PublishedItemType.Element, PublishedItemType.Content })
        {
            try
            {
                var ct = typeCache.Get(itemType, key);
                if (ct is not null)
                {
                    return ct;
                }
            }
            catch
            {
                // try the next bucket
            }
        }

        return null;
    }

    private static JsonObject BuildElementTypeInfo(
        IPublishedContentType elementType,
        IPublishedContentTypeCache typeCache,
        IPropertyEditorSchemaService schemaService,
        int elementTypeDepth)
    {
        var properties = new JsonArray();

        foreach (var propertyType in elementType.PropertyTypes)
        {
            JsonObject? propertySchema = null;
            try
            {
                propertySchema = schemaService.GetValueSchema(
                    propertyType.DataType.EditorAlias,
                    propertyType.DataType.ConfigurationObject);
            }
            catch
            {
                propertySchema = null;
            }

            // Recurse into nested blocks until we run out of depth budget. Bare
            // GUID enums survive at the leaves — the LLM can call
            // get_property_value_schema for deeper drilling if it needs to.
            if (propertySchema is not null && elementTypeDepth > 0)
            {
                EnrichInPlace(propertySchema, typeCache, schemaService, elementTypeDepth - 1);
            }

            properties.Add(new JsonObject
            {
                ["alias"] = propertyType.Alias,
                ["editorAlias"] = propertyType.DataType.EditorAlias,
                ["valueSchema"] = propertySchema,
            });
        }

        return new JsonObject
        {
            ["key"] = elementType.Key.ToString(),
            ["alias"] = elementType.Alias,
            ["properties"] = properties,
        };
    }
}

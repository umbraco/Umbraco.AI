using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Walks a JSON Schema produced by <c>IPropertyEditorSchemaService</c> and adds
/// shallow allow-list metadata next to every <c>contentTypeKey.enum</c>.
/// </summary>
/// <remarks>
/// <para>
/// CMS 17.4.0 emits element-type GUIDs only in <c>contentTypeKey.enum</c>. Without
/// at least the alias of each GUID, an LLM listing "available block types" has no
/// source of truth and hallucinates names from training-data priors. This enricher
/// closes that gap minimally — for each enum entry it attaches a sibling
/// <c>x-allowedElementTypes</c> array of <c>{ key, alias }</c> records and a
/// <c>x-allowedElementTypesNote</c> string that tells the LLM to call
/// <c>get_content_type_schema</c> with an element type's key when it needs the
/// element type's property schemas to author a block of that type.
/// </para>
/// <para>
/// We deliberately do NOT recursively inline each element type's full property
/// schemas. The CMS chose a lazy / on-demand schema model (data-type endpoints
/// pass-through, document-type endpoints use external <c>$ref</c> URIs); mirroring
/// that keeps prompt size bounded — particularly important for the Entity Context
/// block which is loaded into every chat turn — and lets the LLM only pay the
/// token cost for element types it actually needs to author.
/// </para>
/// </remarks>
internal static class BlockSchemaEnricher
{
    private const string AllowedElementTypesNote =
        "Each entry above is an allowed element type for this block list/grid property. " +
        "For an element type's full property schema, call get_content_type_schema with its key. " +
        "Do not author a block of a given element type until you have its property schemas — guessing the values shape will produce malformed content.";

    /// <summary>
    /// Returns an enriched copy of the supplied schema. The original is left untouched.
    /// </summary>
    /// <param name="schema">The bare schema returned by the CMS schema service.</param>
    /// <param name="typeCache">Published-content-type cache used to resolve element-type GUIDs to aliases.</param>
    public static JsonObject? Enrich(JsonObject? schema, IPublishedContentTypeCache typeCache)
    {
        if (schema is null)
        {
            return null;
        }

        // Deep clone via a serialise-roundtrip so the schema service's underlying
        // instance is never mutated. JsonObject.DeepClone exists in .NET 9+, but a
        // roundtrip is simple, robust, and the schemas are small enough that the
        // overhead is irrelevant.
        var clone = JsonNode.Parse(schema.ToJsonString())!.AsObject();
        EnrichInPlace(clone, typeCache);
        return clone;
    }

    private static void EnrichInPlace(JsonNode? node, IPublishedContentTypeCache typeCache)
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

                    if (key == "contentTypeKey"
                        && child is JsonObject contentTypeKeyObj
                        && contentTypeKeyObj["enum"] is JsonArray enumArr
                        && enumArr.Count > 0)
                    {
                        AttachAllowedElementTypes(contentTypeKeyObj, enumArr, typeCache);
                    }
                    else
                    {
                        EnrichInPlace(child, typeCache);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    EnrichInPlace(item, typeCache);
                }
                break;
        }
    }

    private static void AttachAllowedElementTypes(
        JsonObject contentTypeKeyObj,
        JsonArray enumArr,
        IPublishedContentTypeCache typeCache)
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

            allowed.Add(new JsonObject
            {
                ["key"] = elementType.Key.ToString(),
                ["alias"] = elementType.Alias,
            });
        }

        if (allowed.Count > 0)
        {
            contentTypeKeyObj["x-allowedElementTypes"] = allowed;
            contentTypeKeyObj["x-allowedElementTypesNote"] = AllowedElementTypesNote;
        }
    }

    private static IPublishedContentType? TryGetElementType(IPublishedContentTypeCache typeCache, Guid key)
    {
        // Block configurations reference element types, but legacy or shared
        // content types may live under the Content bucket too. Try both with
        // try/catch — the cache throws when an item type isn't registered for
        // a given key.
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
}

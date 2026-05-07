using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Pure helpers for manipulating block-shaped property values (block-list, block-grid, rich-text
/// blocks). Operates on JSON envelopes following the canonical Umbraco shape:
/// <code>
/// {
///   "layout":      { "&lt;layoutKey&gt;": [ &lt;layoutEntry&gt;, ... ] },
///   "contentData":  [ { "key": "...", "contentTypeKey": "...", "values": [...] }, ... ],
///   "settingsData": [ ... ],
///   "expose":       [ { "contentKey": "...", "culture": "...", "segment": "..." }, ... ]
/// }
/// </code>
/// </summary>
/// <remarks>
/// Helpers never mutate inputs; they always return a new <see cref="JsonObject"/> rebuilt from
/// scratch. The <c>layoutKey</c> parameter selects the editor's layout dictionary key
/// (<c>Umbraco.BlockList</c> for block-list, <c>Umbraco.BlockGrid</c> for block-grid). Layout
/// entries are constructed by callers — block-list emits flat <c>{contentKey, settingsKey}</c>
/// entries, block-grid emits the same plus row/column metadata.
/// </remarks>
internal static class BlockEnvelopeOps
{
    public const string ContentDataPropertyName = "contentData";
    public const string SettingsDataPropertyName = "settingsData";
    public const string LayoutPropertyName = "layout";
    public const string ExposePropertyName = "expose";

    public const string ContentKeyPropertyName = "contentKey";
    public const string SettingsKeyPropertyName = "settingsKey";

    /// <summary>
    /// Returns an empty envelope shaped for the given layout key.
    /// </summary>
    public static JsonObject Empty(string layoutKey) => new()
    {
        [LayoutPropertyName] = new JsonObject { [layoutKey] = new JsonArray() },
        [ContentDataPropertyName] = new JsonArray(),
        [SettingsDataPropertyName] = new JsonArray(),
        [ExposePropertyName] = new JsonArray(),
    };

    /// <summary>
    /// Returns the input value as a <see cref="JsonObject"/>, or a freshly-empty envelope when the
    /// input is <c>null</c> or not an object.
    /// </summary>
    public static JsonObject AsEnvelope(JsonNode? value, string layoutKey)
        => value is JsonObject obj ? (JsonObject)obj.DeepClone() : Empty(layoutKey);

    /// <summary>Returns the layout array for the given layout key, creating it if necessary.</summary>
    public static JsonArray GetOrCreateLayoutArray(JsonObject envelope, string layoutKey)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj)
        {
            layoutObj = new JsonObject();
            envelope[LayoutPropertyName] = layoutObj;
        }

        if (layoutObj[layoutKey] is not JsonArray array)
        {
            array = new JsonArray();
            layoutObj[layoutKey] = array;
        }

        return array;
    }

    /// <summary>Returns the contentData/settingsData/expose array, creating it if necessary.</summary>
    public static JsonArray GetOrCreateArray(JsonObject envelope, string propertyName)
    {
        if (envelope[propertyName] is not JsonArray array)
        {
            array = new JsonArray();
            envelope[propertyName] = array;
        }

        return array;
    }

    /// <summary>Adds a content data entry to the envelope and returns its contentKey.</summary>
    /// <remarks>
    /// The <paramref name="values"/> array follows the CMS shape:
    /// <c>[{ alias, value, culture, segment, editorAlias }, ...]</c>.
    /// </remarks>
    public static Guid AddContentDataEntry(
        JsonObject envelope,
        Guid contentTypeKey,
        JsonArray? values,
        Guid? key = null)
    {
        var contentKey = key ?? Guid.NewGuid();
        var entry = new JsonObject
        {
            ["key"] = contentKey,
            ["contentTypeKey"] = contentTypeKey,
            ["values"] = values?.DeepClone() ?? new JsonArray(),
        };

        GetOrCreateArray(envelope, ContentDataPropertyName).Add(entry);
        return contentKey;
    }

    /// <summary>Adds a settings data entry to the envelope and returns its settingsKey.</summary>
    /// <remarks>
    /// The <paramref name="values"/> array follows the CMS shape:
    /// <c>[{ alias, value, culture, segment, editorAlias }, ...]</c>.
    /// </remarks>
    public static Guid AddSettingsDataEntry(
        JsonObject envelope,
        Guid contentTypeKey,
        JsonArray? values,
        Guid? key = null)
    {
        var settingsKey = key ?? Guid.NewGuid();
        var entry = new JsonObject
        {
            ["key"] = settingsKey,
            ["contentTypeKey"] = contentTypeKey,
            ["values"] = values?.DeepClone() ?? new JsonArray(),
        };

        GetOrCreateArray(envelope, SettingsDataPropertyName).Add(entry);
        return settingsKey;
    }

    /// <summary>Adds a layout entry to the layout array, optionally at a specific position.</summary>
    public static void AddLayoutEntry(JsonObject envelope, string layoutKey, JsonObject entry, int? position = null)
    {
        var layoutArray = GetOrCreateLayoutArray(envelope, layoutKey);

        if (position is null || position.Value < 0 || position.Value >= layoutArray.Count)
        {
            layoutArray.Add(entry);
            return;
        }

        var rebuilt = new JsonArray();
        for (var i = 0; i < layoutArray.Count; i++)
        {
            if (i == position.Value)
            {
                rebuilt.Add(entry);
            }

            rebuilt.Add(layoutArray[i]?.DeepClone());
        }

        var layoutObj = (JsonObject)envelope[LayoutPropertyName]!;
        layoutObj[layoutKey] = rebuilt;
    }

    /// <summary>Removes all references to the given content key from layout, contentData, settingsData, and expose.</summary>
    public static void RemoveByContentKey(JsonObject envelope, string layoutKey, Guid contentKey)
    {
        // Resolve the matching settings key (if any) before we lose the reference.
        var settingsKeyToRemove = ResolveSettingsKey(envelope, layoutKey, contentKey);

        // Remove from layout.
        if (envelope[LayoutPropertyName] is JsonObject layoutObj && layoutObj[layoutKey] is JsonArray layoutArray)
        {
            layoutObj[layoutKey] = FilterArray(layoutArray, e => GetGuid(e, ContentKeyPropertyName) != contentKey);
        }

        // Remove from contentData.
        if (envelope[ContentDataPropertyName] is JsonArray contentArray)
        {
            envelope[ContentDataPropertyName] = FilterArray(contentArray, e => GetGuid(e, "key") != contentKey);
        }

        // Remove from settingsData (only if there was a matching settingsKey).
        if (settingsKeyToRemove is not null && envelope[SettingsDataPropertyName] is JsonArray settingsArray)
        {
            envelope[SettingsDataPropertyName] = FilterArray(settingsArray, e => GetGuid(e, "key") != settingsKeyToRemove);
        }

        // Remove from expose.
        if (envelope[ExposePropertyName] is JsonArray exposeArray)
        {
            envelope[ExposePropertyName] = FilterArray(exposeArray, e => GetGuid(e, ContentKeyPropertyName) != contentKey);
        }
    }

    /// <summary>Reorders a layout entry to the given position. No-op if the key is not present.</summary>
    public static void MoveInLayout(JsonObject envelope, string layoutKey, Guid contentKey, int newPosition)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj || layoutObj[layoutKey] is not JsonArray layoutArray)
        {
            return;
        }

        JsonNode? moved = null;
        var rest = new List<JsonNode?>();
        foreach (var entry in layoutArray)
        {
            if (moved is null && GetGuid(entry, ContentKeyPropertyName) == contentKey)
            {
                moved = entry?.DeepClone();
                continue;
            }

            rest.Add(entry?.DeepClone());
        }

        if (moved is null)
        {
            return;
        }

        var clamped = Math.Clamp(newPosition, 0, rest.Count);
        rest.Insert(clamped, moved);

        var rebuilt = new JsonArray();
        foreach (var node in rest)
        {
            rebuilt.Add(node);
        }

        layoutObj[layoutKey] = rebuilt;
    }

    /// <summary>Returns the contentTypeKey of the contentData entry with the given key.</summary>
    public static Guid? FindContentTypeKey(JsonObject envelope, Guid contentKey)
    {
        if (envelope[ContentDataPropertyName] is not JsonArray array)
        {
            return null;
        }

        foreach (var entry in array)
        {
            if (entry is JsonObject obj && GetGuid(obj, "key") == contentKey)
            {
                return GetGuid(obj, "contentTypeKey");
            }
        }

        return null;
    }

    /// <summary>Returns the contentData entry with the given key (live reference into the envelope).</summary>
    public static JsonObject? FindContentDataEntry(JsonObject envelope, Guid contentKey)
    {
        if (envelope[ContentDataPropertyName] is not JsonArray array)
        {
            return null;
        }

        foreach (var entry in array)
        {
            if (entry is JsonObject obj && GetGuid(obj, "key") == contentKey)
            {
                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the value of a property within a content data entry, matching the supplied variant.
    /// </summary>
    /// <remarks>
    /// The CMS values shape is an array of <c>{ alias, value, culture, segment, editorAlias }</c>
    /// objects; we look up the entry whose alias matches and whose culture/segment match the
    /// requested variant (with invariant fallback).
    /// </remarks>
    public static JsonNode? GetPropertyValue(JsonObject contentEntry, string propertyAlias, AIVariantId? variantId)
    {
        if (contentEntry["values"] is not JsonArray values)
        {
            return null;
        }

        JsonNode? invariantFallback = null;

        foreach (var entry in values)
        {
            if (entry is not JsonObject obj)
            {
                continue;
            }

            if (!string.Equals(obj["alias"]?.GetValue<string?>(), propertyAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var culture = obj["culture"]?.GetValue<string?>();
            var segment = obj["segment"]?.GetValue<string?>();

            if (variantId is null || (variantId.Culture == culture && variantId.Segment == segment))
            {
                return obj["value"]?.DeepClone();
            }

            if (culture is null && segment is null)
            {
                invariantFallback = obj["value"]?.DeepClone();
            }
        }

        return invariantFallback;
    }

    /// <summary>
    /// Sets the value of a property within a content data entry, creating or updating the entry as
    /// needed. Mutates the supplied entry in place.
    /// </summary>
    public static void SetPropertyValue(JsonObject contentEntry, string propertyAlias, JsonNode? newValue, AIVariantId? variantId, string? editorAlias = null)
    {
        var values = contentEntry["values"] as JsonArray ?? new JsonArray();
        contentEntry["values"] = values;

        var targetCulture = variantId?.Culture;
        var targetSegment = variantId?.Segment;

        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] is not JsonObject obj)
            {
                continue;
            }

            if (!string.Equals(obj["alias"]?.GetValue<string?>(), propertyAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var entryCulture = obj["culture"]?.GetValue<string?>();
            var entrySegment = obj["segment"]?.GetValue<string?>();

            if (entryCulture == targetCulture && entrySegment == targetSegment)
            {
                obj["value"] = newValue?.DeepClone();
                return;
            }
        }

        var newEntry = new JsonObject
        {
            ["alias"] = propertyAlias,
            ["culture"] = targetCulture,
            ["segment"] = targetSegment,
            ["value"] = newValue?.DeepClone(),
        };
        if (!string.IsNullOrEmpty(editorAlias))
        {
            newEntry["editorAlias"] = editorAlias;
        }

        values.Add(newEntry);
    }

    private static Guid? ResolveSettingsKey(JsonObject envelope, string layoutKey, Guid contentKey)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj || layoutObj[layoutKey] is not JsonArray layoutArray)
        {
            return null;
        }

        foreach (var entry in layoutArray)
        {
            if (entry is JsonObject obj && GetGuid(obj, ContentKeyPropertyName) == contentKey)
            {
                return GetGuid(obj, SettingsKeyPropertyName);
            }
        }

        return null;
    }

    private static JsonArray FilterArray(JsonArray source, Func<JsonNode?, bool> predicate)
    {
        var rebuilt = new JsonArray();
        foreach (var entry in source)
        {
            if (predicate(entry))
            {
                rebuilt.Add(entry?.DeepClone());
            }
        }

        return rebuilt;
    }

    private static Guid? GetGuid(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject obj || obj[propertyName] is not JsonValue value)
        {
            return null;
        }

        // In-memory JsonValue<Guid> values vs string-serialised values both need to round-trip.
        if (value.TryGetValue<Guid>(out var guid))
        {
            return guid;
        }

        if (value.TryGetValue<string>(out var raw) && Guid.TryParse(raw, out guid))
        {
            return guid;
        }

        return null;
    }
}

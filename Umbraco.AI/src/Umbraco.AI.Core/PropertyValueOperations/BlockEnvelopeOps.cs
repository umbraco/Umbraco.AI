using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Pure helpers for manipulating block-shaped property values (block-list, block-grid, rich-text
/// blocks). Operates on JSON envelopes following the canonical Umbraco shape:
/// <code>
/// {
///   "layout":      { "&lt;layoutKey&gt;": [ { "key": "...", "contentKey": "...", "isExternalContent": false }, ... ] },
///   "contentData":  [ { "key": "...", "contentTypeKey": "...", "values": [...] }, ... ],
///   "settingsData": [ ... ],
///   "expose":       [ { "contentKey": "...", "culture": "..." }, ... ]
/// }
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Since CMS 19 a layout item has its own <c>key</c>: reusable (Library) elements mean the same
/// <c>contentKey</c> can appear in several layout items, and an item flagged
/// <c>isExternalContent</c> points at a Library element that is not in <c>contentData</c>.
/// A block is addressed by its layout <c>key</c> first, falling back to its <c>contentKey</c> (the
/// CMS backfills <c>key = contentKey</c> for legacy values, so the two coincide for local blocks).
/// Expose entries are per culture only; segments no longer take part.
/// </para>
/// <para>
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

    public const string KeyPropertyName = "key";
    public const string IsExternalContentPropertyName = "isExternalContent";
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

    /// <summary>
    /// Removes a block: its layout item, and the content/settings data and expose entries that no
    /// other layout item still references.
    /// </summary>
    /// <remarks>
    /// <paramref name="blockKey"/> is matched against the layout <c>key</c> first (removing just that
    /// item, so one of several references to a reusable element can go on its own), then against
    /// <c>contentKey</c> (removing every item that references it).
    /// </remarks>
    public static void RemoveByContentKey(JsonObject envelope, string layoutKey, Guid blockKey)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj || layoutObj[layoutKey] is not JsonArray layoutArray)
        {
            return;
        }

        var removed = SelectLayoutItems(layoutArray, blockKey);
        if (removed.Count == 0)
        {
            return;
        }

        var kept = FilterArray(layoutArray, e => !removed.Any(r => ReferenceEquals(r, e)));
        layoutObj[layoutKey] = kept;

        var remaining = AllLayoutItems(kept).ToList();

        var contentKeysToRemove = removed
            .Select(e => GetGuid(e, ContentKeyPropertyName))
            .OfType<Guid>()
            .Where(k => !remaining.Any(r => GetGuid(r, ContentKeyPropertyName) == k))
            .ToHashSet();
        var settingsKeysToRemove = removed
            .Select(e => GetGuid(e, SettingsKeyPropertyName))
            .OfType<Guid>()
            .Where(k => !remaining.Any(r => GetGuid(r, SettingsKeyPropertyName) == k))
            .ToHashSet();

        if (envelope[ContentDataPropertyName] is JsonArray contentArray)
        {
            envelope[ContentDataPropertyName] = FilterArray(contentArray, e => GetGuid(e, "key") is not { } k || !contentKeysToRemove.Contains(k));
        }

        if (envelope[SettingsDataPropertyName] is JsonArray settingsArray)
        {
            envelope[SettingsDataPropertyName] = FilterArray(settingsArray, e => GetGuid(e, "key") is not { } k || !settingsKeysToRemove.Contains(k));
        }

        if (envelope[ExposePropertyName] is JsonArray exposeArray)
        {
            envelope[ExposePropertyName] = FilterArray(exposeArray, e => GetGuid(e, ContentKeyPropertyName) is not { } k || !contentKeysToRemove.Contains(k));
        }
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="blockKey"/> matches no layout <c>key</c> but several
    /// root layout items share it as their <c>contentKey</c>, so it cannot name a single block.
    /// </summary>
    public static bool IsAmbiguousBlockKey(JsonObject envelope, string layoutKey, Guid blockKey)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj || layoutObj[layoutKey] is not JsonArray layoutArray)
        {
            return false;
        }

        if (layoutArray.Any(e => GetGuid(e, KeyPropertyName) == blockKey))
        {
            return false;
        }

        return layoutArray.Count(e => GetGuid(e, ContentKeyPropertyName) == blockKey) > 1;
    }

    /// <summary>
    /// Validates that <paramref name="blockKey"/> names a single block to remove: rejects a
    /// <c>contentKey</c> shared by several layout items (see <see cref="IsAmbiguousBlockKey"/>).
    /// </summary>
    public static AIValidationResult ValidateRemovable(JsonObject envelope, string layoutKey, Guid blockKey)
        => IsAmbiguousBlockKey(envelope, layoutKey, blockKey)
            ? AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.InvalidPath,
                $"Block key '{blockKey}' is referenced by more than one layout item. Address the block by its layout 'key' instead.",
                Details: new JsonObject { ["blockKey"] = blockKey.ToString() }))
            : AIValidationResult.Valid;

    /// <summary>
    /// Resolves a block key (layout <c>key</c> or <c>contentKey</c>) to the key of the local
    /// <c>contentData</c> entry it edits. Returns <c>null</c> when the matching layout item is
    /// external (Library) content, which has no inline data to edit.
    /// </summary>
    public static Guid? ResolveLocalContentKey(JsonObject envelope, Guid blockKey)
    {
        if (envelope[LayoutPropertyName] is JsonObject layoutObj)
        {
            foreach (var layoutArray in layoutObj.Select(p => p.Value).OfType<JsonArray>())
            {
                var byKey = AllLayoutItems(layoutArray).FirstOrDefault(e => GetGuid(e, KeyPropertyName) == blockKey);
                if (byKey is not null)
                {
                    return IsExternalContent(byKey) ? null : GetGuid(byKey, ContentKeyPropertyName);
                }
            }
        }

        return blockKey;
    }

    /// <summary>Returns <c>true</c> when the layout item points at a Library element rather than inline content.</summary>
    public static bool IsExternalContent(JsonNode? layoutItem)
        => layoutItem is JsonObject obj
           && obj[IsExternalContentPropertyName] is JsonValue value
           && value.TryGetValue<bool>(out var isExternal)
           && isExternal;

    /// <summary>Reorders a layout entry to the given position. No-op if the key is not present.</summary>
    public static void MoveInLayout(JsonObject envelope, string layoutKey, Guid blockKey, int newPosition)
    {
        if (envelope[LayoutPropertyName] is not JsonObject layoutObj || layoutObj[layoutKey] is not JsonArray layoutArray)
        {
            return;
        }

        // Layout key first; with a repeated contentKey the first reference moves.
        var target = layoutArray.FirstOrDefault(e => GetGuid(e, KeyPropertyName) == blockKey)
                     ?? layoutArray.FirstOrDefault(e => GetGuid(e, ContentKeyPropertyName) == blockKey);

        JsonNode? moved = null;
        var rest = new List<JsonNode?>();
        foreach (var entry in layoutArray)
        {
            if (moved is null && ReferenceEquals(entry, target))
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

    /// <summary>Returns the contentTypeKey of the local block addressed by <paramref name="blockKey"/>, or <c>null</c> when it is missing or external.</summary>
    public static Guid? FindContentTypeKey(JsonObject envelope, Guid blockKey)
        => FindContentDataEntry(envelope, blockKey) is { } entry ? GetGuid(entry, "contentTypeKey") : null;

    /// <summary>
    /// Returns the contentData entry of the local block addressed by <paramref name="blockKey"/>
    /// (layout key or content key; live reference into the envelope). <c>null</c> when the block is
    /// missing or is external (Library) content.
    /// </summary>
    public static JsonObject? FindContentDataEntry(JsonObject envelope, Guid blockKey)
    {
        if (envelope[ContentDataPropertyName] is not JsonArray array
            || ResolveLocalContentKey(envelope, blockKey) is not { } contentKey)
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

        JsonNode? defaultSegmentFallback = null;
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

            // A segment with no value of its own falls back to the same culture's default segment
            // (CMS 19 fallback), then to the invariant value.
            if (segment is null && culture is not null && culture == variantId.Culture)
            {
                defaultSegmentFallback = obj["value"]?.DeepClone();
            }
            else if (culture is null && segment is null)
            {
                invariantFallback = obj["value"]?.DeepClone();
            }
        }

        return defaultSegmentFallback ?? invariantFallback;
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

    // Layout items matching a block key: the one whose layout key equals it, else every item whose
    // contentKey does (legacy values without keys, or a bare contentKey address).
    private static List<JsonNode?> SelectLayoutItems(JsonArray layoutArray, Guid blockKey)
    {
        var byKey = layoutArray.Where(e => GetGuid(e, KeyPropertyName) == blockKey).Take(1).ToList();
        return byKey.Count > 0
            ? byKey
            : layoutArray.Where(e => GetGuid(e, ContentKeyPropertyName) == blockKey).ToList();
    }

    // Root items plus block-grid area items, recursively.
    private static IEnumerable<JsonNode?> AllLayoutItems(JsonArray layoutArray)
    {
        foreach (var item in layoutArray)
        {
            yield return item;

            if (item is JsonObject obj && obj["areas"] is JsonArray areas)
            {
                foreach (var area in areas.OfType<JsonObject>())
                {
                    if (area["items"] is JsonArray areaItems)
                    {
                        foreach (var nested in AllLayoutItems(areaItems))
                        {
                            yield return nested;
                        }
                    }
                }
            }
        }
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

    /// <summary>
    /// Reads a GUID property from a JSON object. Handles both in-memory <c>JsonValue&lt;Guid&gt;</c>
    /// nodes (built directly in code) and string-serialised JSON GUIDs (deserialised from the wire),
    /// since both shapes appear in the same envelopes during a single dispatch.
    /// </summary>
    internal static Guid? GetGuid(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject obj || obj[propertyName] is not JsonValue value)
        {
            return null;
        }

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

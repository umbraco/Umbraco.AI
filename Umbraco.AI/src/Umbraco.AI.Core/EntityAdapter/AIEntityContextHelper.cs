using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Default implementation of <see cref="IAIEntityContextHelper"/>.
/// </summary>
internal sealed class AIEntityContextHelper : IAIEntityContextHelper
{
    private readonly AIEntityAdapterCollection _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIEntityContextHelper"/> class.
    /// </summary>
    /// <param name="adapters">The entity adapter collection.</param>
    public AIEntityContextHelper(AIEntityAdapterCollection adapters)
    {
        _adapters = adapters;
    }

    /// <inheritdoc />
    public Dictionary<string, object?> BuildContextDictionary(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var context = new Dictionary<string, object?>
        {
            ["entityType"] = entity.EntityType,
            ["entityId"] = entity.Unique,
            ["entityName"] = entity.Name,
        };

        // Extract contentType from data if present (CMS entities)
        if (entity.Data.ValueKind == JsonValueKind.Object &&
            entity.Data.TryGetProperty("contentType", out var contentTypeElement) &&
            contentTypeElement.ValueKind == JsonValueKind.String)
        {
            context["contentType"] = contentTypeElement.GetString();
        }

        // Extract property values from data.properties array if present (CMS entities).
        //
        // On multi-variant content, the `properties` array carries one entry per
        // (alias, culture, segment). Pick the entry whose culture/segment matches
        // the entity's active variant — falling back to the invariant entry when
        // a property doesn't vary on this content type — so prompt template
        // variables like {{header}} resolve to the active culture's value.
        if (entity.Data.ValueKind == JsonValueKind.Object &&
            entity.Data.TryGetProperty("properties", out var propertiesElement) &&
            propertiesElement.ValueKind == JsonValueKind.Array)
        {
            // Group property entries by alias preserving array order so the
            // last-write-wins fallback below matches the previous behaviour
            // when no culture/segment metadata is present. Aliases are
            // case-insensitive in Umbraco's property model.
            var entriesByAlias = new Dictionary<string, List<JsonElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var propElement in propertiesElement.EnumerateArray())
            {
                if (propElement.ValueKind != JsonValueKind.Object)
                    continue;

                if (!propElement.TryGetProperty("alias", out var aliasElement) ||
                    aliasElement.ValueKind != JsonValueKind.String)
                    continue;

                var alias = aliasElement.GetString();
                if (string.IsNullOrEmpty(alias))
                    continue;

                if (!entriesByAlias.TryGetValue(alias, out var bucket))
                {
                    bucket = new List<JsonElement>();
                    entriesByAlias[alias] = bucket;
                }
                bucket.Add(propElement);
            }

            foreach (var (alias, entries) in entriesByAlias)
            {
                var picked = PickValueForVariant(entries, entity.Culture, entity.Segment);
                if (picked is null)
                    continue;

                object? value = null;
                if (picked.Value.TryGetProperty("value", out var valueElement))
                {
                    value = ExtractValue(valueElement);
                }

                context[alias] = value;
            }
        }

        return context;
    }

    /// <summary>
    /// Pick the property entry that matches the active culture/segment, falling
    /// back to the invariant entry, then the last entry. Mirrors the frontend's
    /// <c>pickValueForVariant</c> in <c>variant-selection.ts</c> so client and
    /// server agree on which value resolves for a given alias. The "last entry"
    /// fallback preserves the pre-fix Map-based last-write-wins behaviour for
    /// payloads without culture metadata.
    /// </summary>
    private static JsonElement? PickValueForVariant(List<JsonElement> entries, string? activeCulture, string? activeSegment)
    {
        if (entries.Count == 0)
            return null;

        // Prefer exact match on (culture, segment).
        foreach (var entry in entries)
        {
            if (GetStringOrNull(entry, "culture") == activeCulture &&
                GetStringOrNull(entry, "segment") == activeSegment)
            {
                return entry;
            }
        }

        // Fall back to the invariant entry.
        foreach (var entry in entries)
        {
            if (GetStringOrNull(entry, "culture") is null && GetStringOrNull(entry, "segment") is null)
            {
                return entry;
            }
        }

        // Last resort: keep the previous "last entry wins" behaviour so payloads
        // without culture metadata continue to resolve.
        return entries[^1];
    }

    /// <summary>
    /// Reads a string property from a JSON object, returning null when the
    /// property is missing or explicitly null. Differs from
    /// <see cref="JsonElement.GetString"/> by also returning null for missing
    /// properties.
    /// </summary>
    private static string? GetStringOrNull(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var element))
            return null;
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <inheritdoc />
    public string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Get the appropriate adapter for this entity type
        var adapter = _adapters.GetAdapter(entity.EntityType);

        return adapter.FormatForLlm(entity);
    }

    /// <inheritdoc />
    public string FormatElementForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Elements (e.g., blocks) use a dedicated format that distinguishes them
        // from the parent entity context
        return Adapters.CmsEntityFormatHelper.FormatCmsElement(entity);
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
}

using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// An element's field values: the invariant ones, or one variant's. A value field holds JSON; an items field holds items,
/// kept apart so each item keeps its key and type.
/// </summary>
public class ElementValues
{
    /// <summary>Values of value fields, by alias.</summary>
    public Dictionary<string, JsonNode?> Fields { get; } = new(StringComparer.Ordinal);

    /// <summary>Items of items fields, by alias.</summary>
    public Dictionary<string, List<EntityItem>> Items { get; } = new(StringComparer.Ordinal);

    /// <summary>An items field's items, created empty if it has none yet.</summary>
    public List<EntityItem> ItemsOf(string alias)
    {
        if (!Items.TryGetValue(alias, out var items))
        {
            items = [];
            Items[alias] = items;
        }

        return items;
    }
}

/// <summary>
/// The entity root or an item: invariant values, plus values per variant (culture and segment). Values are read and
/// written through the field definitions, which say what they vary by, so callers never pick the variant
/// part themselves.
/// </summary>
public class EntityElement : ElementValues
{
    /// <summary>Values that vary, keyed by <see cref="VariantId.Key"/>.</summary>
    public Dictionary<string, ElementValues> Variants { get; } = new(StringComparer.Ordinal);

    /// <summary>The variants this element has values for.</summary>
    public IEnumerable<VariantId> VariantIds => Variants.Keys.Select(VariantId.Parse);

    /// <summary>The values for a variant: the invariant ones for <see cref="VariantId.Invariant"/>, created if missing.</summary>
    public ElementValues Variant(VariantId variant)
    {
        if (variant.IsInvariant)
        {
            return this;
        }

        if (!Variants.TryGetValue(variant.Key, out var values))
        {
            values = new ElementValues();
            Variants[variant.Key] = values;
        }

        return values;
    }

    public ElementValues? VariantOrNull(VariantId variant) => variant.IsInvariant ? this : Variants.GetValueOrDefault(variant.Key);

    /// <summary>A field's value in a variant, narrowed to what the field varies by.</summary>
    public JsonNode? Get(FieldDefinition field, VariantId variant)
        => VariantOrNull(variant.Narrow(field.Variations))?.Fields.GetValueOrDefault(field.Alias);

    public void Set(FieldDefinition field, VariantId variant, JsonNode? value)
        => Variant(variant.Narrow(field.Variations)).Fields[field.Alias] = value?.DeepClone();

    /// <summary>An items field's items in a variant, narrowed to what the field varies by.</summary>
    public List<EntityItem> ItemsFor(FieldDefinition field, VariantId variant) => Variant(variant.Narrow(field.Variations)).ItemsOf(field.Alias);

    /// <summary>A field's value in every variant that has one: once if invariant, else per variant.</summary>
    public IEnumerable<(VariantId Variant, JsonNode Value)> Values(string alias)
    {
        if (Fields.GetValueOrDefault(alias) is { } invariant)
        {
            yield return (VariantId.Invariant, invariant);
        }

        foreach (var (key, values) in Variants)
        {
            if (values.Fields.GetValueOrDefault(alias) is { } value)
            {
                yield return (VariantId.Parse(key), value);
            }
        }
    }

    /// <summary>An items field's items in every variant that has any: once if invariant, else per variant.</summary>
    public IEnumerable<(VariantId Variant, List<EntityItem> Items)> ItemValues(string alias)
    {
        if (Items.GetValueOrDefault(alias) is { Count: > 0 } invariant)
        {
            yield return (VariantId.Invariant, invariant);
        }

        foreach (var (key, values) in Variants)
        {
            if (values.Items.GetValueOrDefault(alias) is { Count: > 0 } items)
            {
                yield return (VariantId.Parse(key), items);
            }
        }
    }

    /// <summary>Replaces this element's values with another's.</summary>
    public void ReplaceWith(EntityElement other)
    {
        var copy = EntityJson.Clone(other);
        Fields.Clear();
        Items.Clear();
        Variants.Clear();
        foreach (var (alias, value) in copy.Fields)
        {
            Fields[alias] = value;
        }

        foreach (var (alias, items) in copy.Items)
        {
            Items[alias] = items;
        }

        foreach (var (key, values) in copy.Variants)
        {
            Variants[key] = values;
        }
    }

    /// <summary>A field's value whether it is invariant or varies: the invariant value, else the first variant's.</summary>
    public JsonNode? ReadAny(string alias) => Values(alias).Select(v => v.Value).FirstOrDefault();
}

/// <summary>An item in an items field: an element with a key and a type, and optionally a settings element and placement values.</summary>
public sealed class EntityItem : EntityElement
{
    public required Guid Key { get; init; }

    public required string Type { get; init; }

    /// <summary>The item's settings element (a block's settings), when its item type has one.</summary>
    public EntityElement? Settings { get; set; }

    /// <summary>The item's placement values in its field (a grid block's column span), when the field has them.</summary>
    public EntityElement? Layout { get; set; }

    /// <summary>Product data no definition describes, kept for round trips (e.g. a CMS block's settings key).</summary>
    public Dictionary<string, string> Extra { get; } = new(StringComparer.Ordinal);

    public EntityElement EnsureSettings() => Settings ??= new EntityElement();

    public EntityElement EnsureLayout() => Layout ??= new EntityElement();
}

/// <summary>
/// A working copy of an entity: its identity and its root element. Serialises to
/// <c>{ "fields": {}, "variants": { "en-US": { "fields": {} } } }</c>, the form tools show, where an items field's value
/// is its list of items.
/// </summary>
public sealed class EntityDocument
{
    public required string EntityType { get; init; }

    public required Guid Key { get; init; }

    /// <summary>The subtype, e.g. a document's content type alias. Null for entity types without subtypes.</summary>
    public string? Subtype { get; init; }

    /// <summary>The stored version this copy was loaded from; null when the entity is new.</summary>
    public long? LiveVersion { get; init; }

    public EntityElement Root { get; init; } = new();

    public EntityDocument Clone() => new()
    {
        EntityType = EntityType,
        Key = Key,
        Subtype = Subtype,
        LiveVersion = LiveVersion,
        Root = EntityJson.Clone(Root),
    };

    /// <summary>The variants the entity has.</summary>
    public IReadOnlyList<VariantId> Variants() => Root.VariantIds.ToList();

    public JsonObject ToJson()
    {
        var json = EntityJson.ToJson(Root);
        json["entityType"] = EntityType;
        json["key"] = Key.ToString();
        json["subtype"] = Subtype;
        json["version"] = LiveVersion;
        return json;
    }
}

/// <summary>The JSON form of elements: what tools return, and how the samples store entities.</summary>
public static class EntityJson
{
    public static JsonObject ToJson(EntityElement element)
    {
        var json = Values(element);
        if (element.Variants.Count > 0)
        {
            json["variants"] = new JsonObject(element.Variants.Select(v => KeyValuePair.Create(v.Key, (JsonNode?)Values(v.Value))));
        }

        if (element is EntityItem item)
        {
            json["key"] = item.Key.ToString();
            json["type"] = item.Type;
            if (item.Settings is { } settings)
            {
                json["settings"] = ToJson(settings);
            }

            if (item.Layout is { } layout)
            {
                json["layout"] = ToJson(layout);
            }

            if (item.Extra.Count > 0)
            {
                json["extra"] = new JsonObject(item.Extra.Select(e => KeyValuePair.Create(e.Key, (JsonNode?)e.Value)));
            }
        }

        return json;
    }

    public static EntityElement FromJson(JsonObject json) => Read(new EntityElement(), json);

    public static EntityItem ItemFromJson(JsonObject json)
    {
        var item = new EntityItem { Key = Guid.Parse(json["key"]!.ToString()), Type = json["type"]!.GetValue<string>() };
        Read(item, json);
        item.Settings = json["settings"] is JsonObject settings ? FromJson(settings) : null;
        item.Layout = json["layout"] is JsonObject layout ? FromJson(layout) : null;
        foreach (var (key, value) in json["extra"] as JsonObject ?? [])
        {
            item.Extra[key] = value!.GetValue<string>();
        }

        return item;
    }

    /// <summary>
    /// Whether a field's JSON is a list of items: a non-empty array whose entries all have a key and a type. Read without
    /// the structure, so a JSON value field shaped exactly like that would be read as items.
    /// </summary>
    private static bool IsItemList(JsonNode? value)
        => value is JsonArray { Count: > 0 } array && array.All(e => e is JsonObject o && o["key"] is not null && o["type"] is JsonValue);

    public static T Clone<T>(T element)
        where T : EntityElement
        => (T)(element is EntityItem ? ItemFromJson(ToJson(element)) : (EntityElement)FromJson(ToJson(element)));

    // One "fields" object: a value field's JSON, or an items field's items (each with its key and type).
    private static JsonObject Values(ElementValues values) => new()
    {
        ["fields"] = new JsonObject(values.Fields.Select(f => KeyValuePair.Create(f.Key, f.Value?.DeepClone()))
            .Concat(values.Items.Where(i => i.Value.Count > 0).Select(i => KeyValuePair.Create(i.Key, (JsonNode?)new JsonArray(i.Value.Select(item => (JsonNode?)ToJson(item)).ToArray()))))),
    };

    private static T Read<T>(T element, JsonObject json)
        where T : EntityElement
    {
        ReadValues(element, json);
        foreach (var (key, variant) in json["variants"] as JsonObject ?? [])
        {
            ReadValues(element.Variant(VariantId.Parse(key)), (JsonObject)variant!);
        }

        return element;
    }

    private static void ReadValues(ElementValues values, JsonObject json)
    {
        foreach (var (alias, value) in json["fields"] as JsonObject ?? [])
        {
            if (IsItemList(value))
            {
                values.Items[alias] = value!.AsArray().OfType<JsonObject>().Select(ItemFromJson).ToList();
            }
            else
            {
                values.Fields[alias] = value?.DeepClone();
            }
        }
    }
}

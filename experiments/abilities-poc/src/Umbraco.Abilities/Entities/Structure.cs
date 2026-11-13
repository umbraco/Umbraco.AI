using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>The value types a field can hold. Kept deliberately small.</summary>
public enum FieldType
{
    String,
    Number,
    Integer,
    Boolean,

    /// <summary>A date as yyyy-MM-dd.</summary>
    Date,

    /// <summary>
    /// The key of another entity. With <see cref="FieldDefinition.ReferencesAnyOf"/> the value is
    /// <c>{ "entityType": "...", "key": "..." }</c>, as a multi-node tree picker can point at documents or media.
    /// </summary>
    EntityReference,

    /// <summary>The key of an item in a slot of the same entity (<see cref="FieldDefinition.ReferencesItemInSlot"/>).</summary>
    ItemReference,

    /// <summary>Any JSON value matching <see cref="FieldDefinition.Schema"/>: the escape hatch for editor values that are not scalars.</summary>
    Json,
}

/// <summary>What a value varies by. Mirrors the CMS's ContentVariation.</summary>
public enum Variance
{
    Invariant,
    Culture,
    Segment,
    CultureAndSegment,
}

public static class VarianceExtensions
{
    public static bool ByCulture(this Variance variance) => variance is Variance.Culture or Variance.CultureAndSegment;

    public static bool BySegment(this Variance variance) => variance is Variance.Segment or Variance.CultureAndSegment;
}

/// <summary>A culture and segment. Either may be null: null culture is invariant, null segment is the default segment.</summary>
public sealed record VariantId(string? Culture, string? Segment)
{
    public static readonly VariantId Invariant = new(null, null);

    public bool IsInvariant => Culture is null && Segment is null;

    /// <summary>The key a variant is stored under in an element's <c>variants</c> object.</summary>
    public string Key => Segment is null ? Culture ?? string.Empty : $"{Culture}|{Segment}";

    /// <summary>Keeps only the parts a value varies by, as the CMS does for each property.</summary>
    public VariantId Narrow(Variance variance) => new(variance.ByCulture() ? Culture : null, variance.BySegment() ? Segment : null);

    public static VariantId Parse(string key)
    {
        var parts = key.Split('|', 2);
        return new VariantId(parts[0].Length == 0 ? null : parts[0], parts.Length == 2 ? parts[1] : null);
    }
}

public sealed record FieldDefinition(string Alias, string Description, FieldType Type, bool Required = false)
{
    public Variance Varies { get; init; } = Variance.Invariant;

    /// <summary>The value is an array of values of <see cref="Type"/> (tags, a multi-node tree picker).</summary>
    public bool Many { get; init; }

    public IReadOnlyList<string>? Allowed { get; init; }

    public string? ReferencesEntityType { get; init; }

    /// <summary>For a reference that can point at several entity types.</summary>
    public IReadOnlyList<string>? ReferencesAnyOf { get; init; }

    /// <summary>A slot on the entity root whose items this field can point at.</summary>
    public string? ReferencesItemInSlot { get; init; }

    /// <summary>The JSON Schema of a <see cref="FieldType.Json"/> value, e.g. a property editor's value schema.</summary>
    public JsonObject? Schema { get; init; }
}

/// <summary>A collection of typed items. Item types come from a registry, so other packages can add them.</summary>
public sealed record SlotDefinition(string Alias, string Description, string ItemTypeGroup)
{
    public int? Min { get; init; }

    public int? Max { get; init; }

    /// <summary>A culture-variant slot holds a separate list per culture, like a culture-variant block property.</summary>
    public Variance Varies { get; init; } = Variance.Invariant;

    /// <summary>
    /// Values that belong to an item's place in this slot rather than to the item, such as a block grid's
    /// column and row span. Edited at <c>{itemPath}/@layout</c>.
    /// </summary>
    public ElementShape? ItemLayout { get; init; }
}

/// <summary>The shape of the entity root or of an item: fields plus slots. Items can have slots, so shapes nest.</summary>
public sealed record ElementShape(IReadOnlyList<FieldDefinition> Fields, IReadOnlyList<SlotDefinition> Slots)
{
    public static ElementShape Of(params FieldDefinition[] fields) => new(fields, []);

    public ElementShape WithSlots(params SlotDefinition[] slots) => this with { Slots = slots };
}

public sealed record EntityStructure(string EntityType, string Name, string Description, ElementShape Shape)
{
    /// <summary>Whether the entity has variants at all (a culture-variant document type).</summary>
    public Variance Varies { get; init; } = Variance.Invariant;

    /// <summary>The cultures the entity can have, e.g. the site's languages. Segments are not listed: the CMS doesn't manage them.</summary>
    public IReadOnlyList<string> Cultures { get; init; } = [];
}

public sealed record ItemTypeDefinition(string Group, string Alias, string Name, string Description, ElementShape Shape)
{
    /// <summary>A second element per item, edited at <c>{itemPath}/@settings</c>, like a block's settings element type.</summary>
    public ElementShape? SettingsShape { get; init; }

    /// <summary>Further groups the type belongs to, e.g. a grid block allowed both at the root and in areas.</summary>
    public IReadOnlyList<string> AlsoIn { get; init; } = [];
}

/// <summary>Item types by group. Aliases are unique across the registry.</summary>
public sealed class ItemTypeRegistry
{
    private readonly Dictionary<string, ItemTypeDefinition> _byAlias = new(StringComparer.Ordinal);

    public ItemTypeRegistry Add(ItemTypeDefinition itemType)
    {
        _byAlias.Add(itemType.Alias, itemType);
        return this;
    }

    public ItemTypeDefinition? Find(string alias) => _byAlias.GetValueOrDefault(alias);

    public IReadOnlyList<ItemTypeDefinition> InGroup(string group)
        => _byAlias.Values.Where(t => t.Group == group || t.AlsoIn.Contains(group)).ToList();
}

/// <summary>
/// A working copy of an entity. The root and every item are elements:
/// <c>{ "fields": {}, "slots": {}, "variants": { "en-US": { "fields": {}, "slots": {} } } }</c>.
/// Invariant values sit in <c>fields</c>/<c>slots</c>; values that vary sit under their variant's key.
/// An item also has <c>key</c> and <c>type</c>, and optionally <c>settings</c> and <c>layout</c> elements.
/// </summary>
public sealed class EntityDocument
{
    public required string EntityType { get; init; }

    public required Guid Key { get; init; }

    /// <summary>The stored version this copy was loaded from; null when the entity is new.</summary>
    public long? LiveVersion { get; init; }

    public JsonObject Root { get; init; } = Element.New();

    public EntityDocument Clone() => new()
    {
        EntityType = EntityType,
        Key = Key,
        LiveVersion = LiveVersion,
        Root = (JsonObject)Root.DeepClone(),
    };

    public JsonObject ToJson()
    {
        var json = (JsonObject)Root.DeepClone();
        json["entityType"] = EntityType;
        json["key"] = Key.ToString();
        json["version"] = LiveVersion;
        return json;
    }

    /// <summary>The variants the entity has, taken from the root's <c>variants</c>.</summary>
    public IReadOnlyList<VariantId> Variants()
        => Root["variants"] is JsonObject variants ? variants.Select(v => VariantId.Parse(v.Key)).ToList() : [];
}

public static class Element
{
    public static JsonObject New() => new()
    {
        ["fields"] = new JsonObject(),
        ["slots"] = new JsonObject(),
        ["variants"] = new JsonObject(),
    };

    public static JsonObject NewItem(Guid key, string type)
    {
        var item = New();
        item["key"] = key.ToString();
        item["type"] = type;
        return item;
    }

    /// <summary>The element's invariant fields.</summary>
    public static JsonObject Fields(JsonObject element) => Child(element, "fields");

    /// <summary>The invariant list of a slot, created if missing.</summary>
    public static JsonArray Slot(JsonObject element, string alias)
    {
        var slots = Child(element, "slots");
        if (slots[alias] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        slots[alias] = created;
        return created;
    }

    /// <summary>The part of an element that holds one variant's values, created if missing.</summary>
    public static JsonObject VariantPart(JsonObject element, VariantId variant)
    {
        var variants = Child(element, "variants");
        if (variants[variant.Key] is JsonObject existing)
        {
            return existing;
        }

        var part = new JsonObject { ["fields"] = new JsonObject(), ["slots"] = new JsonObject() };
        variants[variant.Key] = part;
        return part;
    }

    public static JsonObject? VariantPartOrNull(JsonObject element, VariantId variant)
        => element["variants"]?[variant.Key] as JsonObject;

    /// <summary>Where a field's value lives for a variant: the invariant fields, or the variant's part.</summary>
    public static JsonObject FieldsFor(JsonObject element, FieldDefinition field, VariantId variant)
        => field.Varies == Variance.Invariant ? Fields(element) : Fields(VariantPart(element, variant.Narrow(field.Varies)));

    public static JsonArray SlotFor(JsonObject element, SlotDefinition slot, VariantId variant)
        => slot.Varies == Variance.Invariant ? Slot(element, slot.Alias) : Slot(VariantPart(element, variant.Narrow(slot.Varies)), slot.Alias);

    /// <summary>An item's settings or layout element, created if missing.</summary>
    public static JsonObject SubElement(JsonObject item, string name) => Child(item, name, New);

    /// <summary>Reads a field whether it is invariant or varies: the invariant value, else the first variant's.</summary>
    public static JsonNode? ReadAny(JsonObject element, string alias)
        => element["fields"]?[alias]
            ?? (element["variants"] as JsonObject)?.Select(v => v.Value?["fields"]?[alias]).FirstOrDefault(v => v is not null);

    public static Guid ItemKey(JsonNode item) => Guid.Parse(item["key"]!.GetValue<string>());

    public static string ItemType(JsonNode item) => item["type"]!.GetValue<string>();

    private static JsonObject Child(JsonObject element, string name, Func<JsonObject>? create = null)
    {
        if (element[name] is JsonObject existing)
        {
            return existing;
        }

        var created = create?.Invoke() ?? new JsonObject();
        element[name] = created;
        return created;
    }
}

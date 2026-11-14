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
public enum Variation
{
    Invariant,
    Culture,
    Segment,
    CultureAndSegment,
}

public static class VariationExtensions
{
    public static bool VariesByCulture(this Variation variation) => variation is Variation.Culture or Variation.CultureAndSegment;

    public static bool VariesBySegment(this Variation variation) => variation is Variation.Segment or Variation.CultureAndSegment;
}

/// <summary>A culture and segment. Either may be null: null culture is invariant, null segment is the default segment.</summary>
public sealed record VariantId(string? Culture, string? Segment)
{
    public static readonly VariantId Invariant = new(null, null);

    public bool IsInvariant => Culture is null && Segment is null;

    /// <summary>The key a variant is stored under in an element's <c>variants</c> object.</summary>
    public string Key => Segment is null ? Culture ?? string.Empty : $"{Culture}|{Segment}";

    /// <summary>Keeps only the parts a value varies by, as the CMS does for each property.</summary>
    public VariantId Narrow(Variation variation) => new(variation.VariesByCulture() ? Culture : null, variation.VariesBySegment() ? Segment : null);

    public static VariantId Parse(string key)
    {
        var parts = key.Split('|', 2);
        return new VariantId(parts[0].Length == 0 ? null : parts[0], parts.Length == 2 ? parts[1] : null);
    }
}

public sealed record FieldDefinition(string Alias, string Description, FieldType Type, bool Required = false)
{
    public Variation Variations { get; init; } = Variation.Invariant;

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
    public Variation Variations { get; init; } = Variation.Invariant;

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
    public Variation Variations { get; init; } = Variation.Invariant;

    /// <summary>The cultures the entity can have, e.g. the site's languages. Segments are not listed: the CMS doesn't manage them.</summary>
    public IReadOnlyList<string> Cultures { get; init; } = [];
}

public sealed record ItemTypeDefinition(string Group, string Alias, string Name, string Description, ElementShape Shape)
{
    /// <summary>A second element per item, edited at <c>{itemPath}/@settings</c>, like a block's settings element type.</summary>
    public ElementShape? SettingsShape { get; init; }

    /// <summary>Further groups the type belongs to, e.g. a grid block allowed both at the root and in areas.</summary>
    public IReadOnlyList<string> AlsoIn { get; init; } = [];

    /// <summary>The model class for items of this type, when it was registered from one.</summary>
    public Type? ClrType { get; init; }
}

/// <summary>
/// Item types by group. An alias is unique within a group, but the same alias can appear in several groups,
/// as one element type can be used by several block editors with different settings and areas.
/// </summary>
public sealed class ItemTypeRegistry
{
    private readonly Dictionary<(string Group, string Alias), ItemTypeDefinition> _types = [];

    public ItemTypeRegistry Add(ItemTypeDefinition itemType)
    {
        _types.Add((itemType.Group, itemType.Alias), itemType);
        return this;
    }

    /// <summary>
    /// Adds an item type from its model class: the alias, name and description from <c>[ItemType]</c>, the fields and
    /// nested slots from its properties, and the settings from its <c>[ItemSettings]</c> property.
    /// </summary>
    public ItemTypeRegistry Add<T>(string group, params string[] alsoIn)
        where T : class
    {
        var info = Models.ModelShapes.Info(typeof(T));
        var (alias, name, description) = info.ItemType
            ?? throw new InvalidOperationException($"{typeof(T).Name} is not an item type: add [ItemType] or configure it with IsItemType.");
        if (info.Key is null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} has no key: derive from EntityItemModel, add [Key] or configure HasKey.");
        }

        return Add(new ItemTypeDefinition(group, alias, name, description, info.Shape)
        {
            SettingsShape = info.Settings is { } settings ? Models.ModelShapes.ShapeOf(settings.PropertyType) : null,
            AlsoIn = alsoIn,
            ClrType = typeof(T),
        });
    }

    /// <summary>Adds the type unless one is already registered for its group and alias.</summary>
    public ItemTypeRegistry AddIfMissing(ItemTypeDefinition itemType)
    {
        _types.TryAdd((itemType.Group, itemType.Alias), itemType);
        return this;
    }

    /// <summary>Finds a type by alias, or by <c>group:alias</c> when the alias is used in several groups.</summary>
    public ItemTypeDefinition? Find(string alias)
    {
        var separator = alias.LastIndexOf(':');
        return separator > 0
            ? Find(alias[..separator], alias[(separator + 1)..])
            : _types.Values.FirstOrDefault(t => t.Alias == alias);
    }

    public ItemTypeDefinition? Find(string group, string alias)
        => InGroup(group).FirstOrDefault(t => t.Alias == alias);

    public IReadOnlyList<ItemTypeDefinition> InGroup(string group)
        => _types.Values.Where(t => t.Group == group || t.AlsoIn.Contains(group)).ToList();
}


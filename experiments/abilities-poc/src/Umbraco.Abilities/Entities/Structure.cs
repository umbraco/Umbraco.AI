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

    /// <summary>The key of an item in an items field of the same entity (<see cref="FieldDefinition.ReferencesItemIn"/>).</summary>
    ItemReference,

    /// <summary>Any JSON value matching <see cref="FieldDefinition.Schema"/>, set whole: for editor values the framework doesn't describe.</summary>
    Json,

    /// <summary>An object whose fields are described (<see cref="FieldDefinition.Shape"/>), so paths go into it: <c>seo/title</c>, a block's <c>settings/theme</c>.</summary>
    Object,

    /// <summary>
    /// An array of items: objects with a <c>key</c> and a <c>type</c>, the type registered for
    /// <see cref="FieldDefinition.ItemTypeGroup"/>. A Block List, discount rules, workflow steps. Paths step into an item by
    /// its key (<c>rules/{key}/minTotal</c>), so it can be edited, reported on and rebased on its own.
    /// </summary>
    Items,
}

/// <summary>The dimensions Umbraco content varies by. Other products name their own.</summary>
public static class VariationDimensions
{
    public const string Culture = "culture";
    public const string Segment = "segment";
}

/// <summary>
/// Something values can vary by: the CMS's culture and segment, or a product's own (a price per currency, a setting per
/// store). The structure lists the dimensions an entity has; each field says which of them it varies by.
/// </summary>
/// <param name="Values">The values allowed, such as the site's languages; null when any value is allowed (the CMS's segments).</param>
public sealed record VariationDimension(string Name, string Description, IReadOnlyList<string>? Values = null);

/// <summary>
/// Where a value is, among the dimensions it varies by: <c>{ culture: "da-DK" }</c>, <c>{ culture: "da-DK", segment: "vip" }</c>,
/// <c>{ currency: "EUR" }</c>. A dimension with no value is its default (the invariant culture, the default segment).
/// </summary>
public sealed class VariantId : IEquatable<VariantId>
{
    private readonly SortedDictionary<string, string> _values;

    public VariantId(IEnumerable<KeyValuePair<string, string?>>? values = null)
        => _values = new SortedDictionary<string, string>((values ?? []).Where(v => v.Value is not null).ToDictionary(v => v.Key, v => v.Value!), StringComparer.Ordinal);

    public static readonly VariantId Invariant = new();

    public static VariantId Of(string dimension, string? value) => new([KeyValuePair.Create(dimension, value)]);

    /// <summary>A variant from its JSON form, <c>{ "culture": "da-DK" }</c>; invariant for null.</summary>
    public static VariantId From(IReadOnlyDictionary<string, string>? values)
        => new(values?.Select(v => KeyValuePair.Create(v.Key, (string?)v.Value)));

    public string? this[string dimension] => _values.GetValueOrDefault(dimension);

    public IReadOnlyDictionary<string, string> Values => _values;

    public bool IsInvariant => _values.Count == 0;

    /// <summary>This variant with one dimension's value set, or removed when null.</summary>
    public VariantId With(string dimension, string? value)
        => new(_values.Where(v => v.Key != dimension).Select(v => KeyValuePair.Create(v.Key, (string?)v.Value)).Append(KeyValuePair.Create(dimension, value)));

    /// <summary>Keeps only the dimensions a value varies by, as the CMS does for each property.</summary>
    public VariantId Narrow(IReadOnlyList<string> variesBy) => new(variesBy.Select(d => KeyValuePair.Create(d, this[d])));

    /// <summary>This variant's values on top of <paramref name="context"/>'s: a value inside an item takes the item's variant.</summary>
    public VariantId Over(VariantId context) => new(context._values.Concat(_values).GroupBy(v => v.Key).Select(g => KeyValuePair.Create(g.Key, (string?)g.Last().Value)));

    /// <summary>Whether a value in this variant is seen in <paramref name="other"/>: every dimension this one sets, the other has too.</summary>
    public bool Within(VariantId other) => _values.All(v => other[v.Key] == v.Value);

    /// <summary>The key a varying value is stored under: its dimensions' values in the field's order, <c>"da-DK"</c> or <c>"da-DK|vip"</c>.</summary>
    public string KeyFor(IReadOnlyList<string> variesBy) => string.Join('|', variesBy.Select(d => this[d] ?? string.Empty)).TrimEnd('|');

    public static VariantId FromKey(string key, IReadOnlyList<string> variesBy)
    {
        var parts = key.Split('|');
        return new(variesBy.Select((d, i) => KeyValuePair.Create(d, i < parts.Length && parts[i].Length > 0 ? parts[i] : null)));
    }

    /// <summary>One string for the whole variant, <c>"culture=da-DK;segment=vip"</c>: for keys that cross fields, such as a conflict's.</summary>
    public string Key => string.Join(';', _values.Select(v => $"{v.Key}={v.Value}"));

    public static VariantId Parse(string key)
        => new(key.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).Select(p => KeyValuePair.Create(p[0], (string?)p[1])));

    /// <summary>The variant as a problem or conflict shows it: null when invariant.</summary>
    public IReadOnlyDictionary<string, string>? ToJson() => IsInvariant ? null : new Dictionary<string, string>(_values);

    public bool Equals(VariantId? other) => other is not null && Key == other.Key;

    public override bool Equals(object? obj) => Equals(obj as VariantId);

    public override int GetHashCode() => Key.GetHashCode(StringComparison.Ordinal);

    /// <summary>The values, for showing to people: <c>"da-DK"</c>, <c>"da-DK, vip"</c>.</summary>
    public override string ToString() => IsInvariant ? "invariant" : string.Join(", ", _values.Values);
}

public sealed record FieldDefinition(string Alias, string Description, FieldType Type, bool Required = false)
{
    /// <summary>The dimensions the value varies by, e.g. <c>["culture"]</c>. Empty: one value for all.</summary>
    public IReadOnlyList<string> VariesBy { get; init; } = [];

    public bool Varies => VariesBy.Count > 0;

    /// <summary>The value is an array of values of <see cref="Type"/> (tags, a multi-node tree picker).</summary>
    public bool Many { get; init; }

    public IReadOnlyList<string>? Allowed { get; init; }

    public string? ReferencesEntityType { get; init; }

    /// <summary>For a reference that can point at several entity types.</summary>
    public IReadOnlyList<string>? ReferencesAnyOf { get; init; }

    /// <summary>An items field on the entity root whose items this field can point at.</summary>
    public string? ReferencesItemIn { get; init; }

    /// <summary>The JSON Schema of a <see cref="FieldType.Json"/> value, e.g. a property editor's value schema.</summary>
    public JsonObject? Schema { get; init; }

    /// <summary>
    /// DataAnnotations rules beyond required and allowed values (a range, a pattern), checked on the field's path. For
    /// item types built from a product's own setting metadata, which has no model class to carry them.
    /// </summary>
    public IReadOnlyList<System.ComponentModel.DataAnnotations.ValidationAttribute>? Rules { get; init; }

    /// <summary>The value can be a binding to an earlier step's output, e.g. <c>{{trigger.orderId}}</c>, instead of a literal.</summary>
    public bool AcceptsBindings { get; init; }

    /// <summary>
    /// The handler that owns the value's inner model, for a value type whose editor defines one (Rich Text's markup and
    /// blocks). Paths into the field go to it. Without one, the value is set whole.
    /// </summary>
    public IValueHandler? Handler { get; init; }

    // ── For FieldType.Items ──────────────────────────────────────────────────────────────────────

    /// <summary>The group whose registered item types the field holds. Item types come from a registry, so other packages add them.</summary>
    public string? ItemTypeGroup { get; init; }

    /// <summary>The fewest items.</summary>
    public int? Min { get; init; }

    /// <summary>The most items. 1 for a single item.</summary>
    public int? Max { get; init; }

    // ── For FieldType.Object ─────────────────────────────────────────────────────────────────────

    /// <summary>The object's fields.</summary>
    public ElementShape? Shape { get; init; }

    public bool IsItems => Type == FieldType.Items;

    /// <summary>An items field holding items of a group.</summary>
    public static FieldDefinition ItemsOf(string alias, string description, string itemTypeGroup)
        => new(alias, description, FieldType.Items) { ItemTypeGroup = itemTypeGroup };

    /// <summary>An object field with its own fields.</summary>
    public static FieldDefinition ObjectOf(string alias, string description, ElementShape shape)
        => new(alias, description, FieldType.Object) { Shape = shape };
}

/// <summary>The fields of an object: the entity, an item, or an object value. Items and objects have fields of their own, so shapes nest.</summary>
public sealed record ElementShape(IReadOnlyList<FieldDefinition> Fields)
{
    public static ElementShape Of(params FieldDefinition[] fields) => new(fields);

    public FieldDefinition? Find(string alias) => Fields.FirstOrDefault(f => f.Alias == alias);

    /// <summary>The fields holding items.</summary>
    public IEnumerable<FieldDefinition> ItemFields => Fields.Where(f => f.IsItems);
}

public sealed record EntityStructure(string EntityType, string Name, string Description, ElementShape Shape)
{
    /// <summary>What the entity's values can vary by, e.g. culture with the site's languages. Empty when nothing varies.</summary>
    public IReadOnlyList<VariationDimension> Dimensions { get; init; } = [];

    public VariationDimension? Dimension(string name) => Dimensions.FirstOrDefault(d => d.Name == name);
}

public sealed record ItemTypeDefinition(string Group, string Alias, string Name, string Description, ElementShape Shape)
{
    /// <summary>Further groups the type belongs to, e.g. a grid block allowed both at the root and in areas.</summary>
    public IReadOnlyList<string> AlsoIn { get; init; } = [];

    /// <summary>The model class for items of this type, when it was registered from one.</summary>
    public Type? ClrType { get; init; }

    /// <summary>Whether a user may use this type, e.g. an Automate action that needs the Commerce section. Null: everyone.</summary>
    public Func<Guid, bool>? AvailableTo { get; init; }

    public bool IsAvailableTo(Guid userKey) => AvailableTo?.Invoke(userKey) ?? true;
}

/// <summary>
/// The item types of a plugin seam, read from the product's own registry each time they're needed: Commerce's discount
/// rule providers, Automate's triggers and actions. A product writes one source per seam; plugins write nothing, and one
/// installed later just shows up.
/// </summary>
public interface IItemTypeSource
{
    string Group { get; }

    IEnumerable<ItemTypeDefinition> GetItemTypes();
}

/// <summary>
/// Item types by group. An alias is unique within a group, but the same alias can appear in several groups,
/// as one element type can be used by several block editors with different settings and areas.
/// </summary>
public sealed class ItemTypeRegistry
{
    private readonly Dictionary<(string Group, string Alias), ItemTypeDefinition> _types = [];
    private readonly List<IItemTypeSource> _sources = [];

    /// <summary>Adds a plugin seam's item types, read from the product's registry whenever they're needed.</summary>
    public ItemTypeRegistry AddSource(IItemTypeSource source)
    {
        _sources.Add(source);
        return this;
    }

    private IEnumerable<ItemTypeDefinition> All => _types.Values.Concat(_sources.SelectMany(s => s.GetItemTypes()));

    public ItemTypeRegistry Add(ItemTypeDefinition itemType)
    {
        _types.Add((itemType.Group, itemType.Alias), itemType);
        return this;
    }

    /// <summary>
    /// Adds an item type from its model class: the alias, name and description from <c>[ItemType]</c>, and the fields
    /// from its properties.
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
            : All.FirstOrDefault(t => t.Alias == alias);
    }

    public ItemTypeDefinition? Find(string group, string alias)
        => InGroup(group).FirstOrDefault(t => t.Alias == alias);

    public IReadOnlyList<ItemTypeDefinition> InGroup(string group)
        => _types.Values.Where(t => t.Group == group || t.AlsoIn.Contains(group))
            .Concat(_sources.Where(s => s.Group == group).SelectMany(s => s.GetItemTypes()))
            .ToList();
}


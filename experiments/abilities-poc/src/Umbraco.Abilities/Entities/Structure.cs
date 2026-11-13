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

    /// <summary>The key of another entity (<see cref="FieldDefinition.ReferencesEntityType"/>).</summary>
    EntityReference,

    /// <summary>The key of an item in a slot of the same entity (<see cref="FieldDefinition.ReferencesItemInSlot"/>).</summary>
    ItemReference,
}

public sealed record FieldDefinition(string Alias, string Description, FieldType Type, bool Required = false)
{
    public IReadOnlyList<string>? Allowed { get; init; }

    public string? ReferencesEntityType { get; init; }

    /// <summary>A slot on the entity root whose items this field can point at. This is how graphs fit.</summary>
    public string? ReferencesItemInSlot { get; init; }
}

/// <summary>A collection of typed items. Item types come from a registry, so other packages can add them.</summary>
public sealed record SlotDefinition(string Alias, string Description, string ItemTypeGroup)
{
    public int? Min { get; init; }

    public int? Max { get; init; }
}

/// <summary>The shape of the entity root or of an item: fields plus slots. Items can have slots, so shapes nest.</summary>
public sealed record ElementShape(IReadOnlyList<FieldDefinition> Fields, IReadOnlyList<SlotDefinition> Slots)
{
    public static ElementShape Of(params FieldDefinition[] fields) => new(fields, []);

    public ElementShape WithSlots(params SlotDefinition[] slots) => this with { Slots = slots };
}

public sealed record EntityStructure(string EntityType, string Name, string Description, ElementShape Shape);

public sealed record ItemTypeDefinition(string Group, string Alias, string Name, string Description, ElementShape Shape);

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
        => _byAlias.Values.Where(t => t.Group == group).ToList();
}

/// <summary>
/// A working copy of an entity. The root and every item are elements: <c>{ "fields": {}, "slots": {} }</c>;
/// an item also has <c>key</c> and <c>type</c>.
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

    public JsonObject ToJson() => new()
    {
        ["entityType"] = EntityType,
        ["key"] = Key.ToString(),
        ["version"] = LiveVersion,
        ["fields"] = Root["fields"]!.DeepClone(),
        ["slots"] = Root["slots"]!.DeepClone(),
    };
}

public static class Element
{
    public static JsonObject New() => new() { ["fields"] = new JsonObject(), ["slots"] = new JsonObject() };

    public static JsonObject NewItem(Guid key, string type) => new()
    {
        ["key"] = key.ToString(),
        ["type"] = type,
        ["fields"] = new JsonObject(),
        ["slots"] = new JsonObject(),
    };

    public static JsonObject Fields(JsonObject element) => (JsonObject)element["fields"]!;

    public static JsonArray Slot(JsonObject element, string alias)
    {
        var slots = (JsonObject)element["slots"]!;
        if (slots[alias] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        slots[alias] = created;
        return created;
    }

    public static Guid ItemKey(JsonNode item) => Guid.Parse(item["key"]!.GetValue<string>());

    public static string ItemType(JsonNode item) => item["type"]!.GetValue<string>();
}

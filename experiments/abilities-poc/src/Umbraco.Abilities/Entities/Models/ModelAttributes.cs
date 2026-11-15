using System.Text.Json.Serialization;

using Umbraco.Extensions;
namespace Umbraco.Abilities.Entities.Models;

/// <summary>Declares a C# class as an entity type's model.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute(string entityType, string name) : Attribute
{
    public string EntityType { get; } = entityType;

    public string Name { get; } = name;

    public string Description { get; set; } = string.Empty;
}

/// <summary>The property that holds the entity's display name. Can be a <see cref="Varying{T}"/> for per-culture names.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EntityNameAttribute : Attribute
{
}

/// <summary>The property that holds the entity's stored version, for rebasing change sets. Set when loaded.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EntityVersionAttribute : Attribute
{
}

/// <summary>Declares a class as an item type. Register it with <see cref="ItemTypeRegistry.Add{T}"/> under a group.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ItemTypeAttribute(string alias, string name) : Attribute
{
    public string Alias { get; } = alias;

    public string Name { get; } = name;

    public string Description { get; set; } = string.Empty;
}

/// <summary>An items field: a list property holding items of the types registered for <paramref name="itemTypeGroup"/>.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ItemsAttribute(string itemTypeGroup) : Attribute
{
    public string ItemTypeGroup { get; } = itemTypeGroup;

    /// <summary>Minimum number of items; -1 for none.</summary>
    public int Min { get; set; } = -1;

    /// <summary>Maximum number of items; -1 for none.</summary>
    public int Max { get; set; } = -1;
}

/// <summary>
/// The handler that adds behaviour to an object property's value beyond its fields (Rich Text keeping its markup's
/// block tags in step with its blocks). The handler type needs a parameterless constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValueHandlerAttribute(Type handlerType) : Attribute
{
    public Type HandlerType { get; } = handlerType;
}

/// <summary>The base class for item models. The key identifies the item within its field; leave it empty for a new item.</summary>
public abstract class EntityItemModel
{
    public Guid Key { get; set; }

    /// <summary>Where the item sits in the entity, e.g. <c>"rewards/{key}"</c>. Set when the model is read; use it for problem paths.</summary>
    [JsonIgnore]
    public string Path { get; internal set; } = string.Empty;
}

/// <summary>
/// An item whose type has no model class: a plugin's item type, such as a Commerce discount rule provider or an Automate
/// action, described by its product's registry at run time. A model class declares an items field of these
/// (<c>List&lt;DynamicItem&gt;</c>) when its item types are a plugin seam. Fields are the item's values as JSON, items fields included.
/// </summary>
public class DynamicItem : EntityItemModel
{
    public string Type { get; set; } = string.Empty;

    public Dictionary<string, System.Text.Json.Nodes.JsonNode?> Fields { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>What a <see cref="Varying{T}"/> property varies by, when not culture: e.g. <c>[VariesBy("currency")]</c>.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VariesByAttribute(params string[] dimensions) : Attribute
{
    public IReadOnlyList<string> Dimensions { get; } = dimensions;
}

/// <summary>
/// A value per variant, keyed by its dimensions' values: <c>"en-US"</c>, or <c>"en-US|vip"</c> for culture and segment.
/// A property of this type varies by culture unless <see cref="VariesByAttribute"/> says otherwise.
/// </summary>
public sealed class Varying<T> : Dictionary<string, T>
{
    public Varying()
        : base(StringComparer.Ordinal)
    {
    }
}

/// <summary>A reference to an entity of one of several types, e.g. a tree picker that can pick pages or media.</summary>
public sealed record EntityReference(string EntityType, Guid Key);

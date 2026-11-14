using System.Text.Json.Serialization;

using Umbraco.Cms.Core.Models;
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

/// <summary>A list property that holds items of the types registered for <paramref name="itemTypeGroup"/>.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SlotAttribute(string itemTypeGroup) : Attribute
{
    public string ItemTypeGroup { get; } = itemTypeGroup;

    /// <summary>Minimum number of items; -1 for none.</summary>
    public int Min { get; set; } = -1;

    /// <summary>Maximum number of items; -1 for none.</summary>
    public int Max { get; set; } = -1;

    /// <summary>A class describing the values an item has because of its place in this slot, e.g. a grid's column span.</summary>
    public Type? Layout { get; set; }
}

/// <summary>On an item class: the property that holds the item's settings element (a block's settings).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ItemSettingsAttribute : Attribute
{
}

/// <summary>On an item class: the property that holds the item's placement values, of the slot's layout type.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ItemLayoutAttribute : Attribute
{
}

/// <summary>A key of another item in the entity's root slot <paramref name="slot"/>: how graph edges are modelled.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ItemReferenceAttribute(string slot) : Attribute
{
    public string Slot { get; } = slot;
}

/// <summary>What a <see cref="Varying{T}"/> property varies by, when not by culture.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VariationsAttribute(ContentVariation variations) : Attribute
{
    public ContentVariation Variations { get; } = variations;
}

/// <summary>The base class for item models. The key identifies the item within its slot; leave it empty for a new item.</summary>
public abstract class EntityItemModel
{
    public Guid Key { get; set; }

    /// <summary>Where the item sits in the entity, e.g. <c>"rewards/{key}"</c>. Set when the model is read; use it for problem paths.</summary>
    [JsonIgnore]
    public string Path { get; internal set; } = string.Empty;
}

/// <summary>
/// An item whose type has no model class: a plugin's item type, such as a Commerce discount rule provider or an Automate
/// action, described by its product's registry at run time. A model class declares a slot of these
/// (<c>List&lt;DynamicItem&gt;</c>) when the slot's item types are a plugin seam. Values are the item's JSON fields.
/// </summary>
public class DynamicItem : EntityItemModel
{
    public string Type { get; set; } = string.Empty;

    public Dictionary<string, System.Text.Json.Nodes.JsonNode?> Fields { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Nested slots, e.g. a rule group's child rules.</summary>
    public Dictionary<string, List<DynamicItem>> Slots { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// A value per variant, keyed by culture (<c>"en-US"</c>), or <c>"culture|segment"</c> when it also varies by segment.
/// A property of this type varies by culture unless <see cref="VariationsAttribute"/> says otherwise.
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

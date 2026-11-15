using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Behaviour a value's schema can't express. Paths and storage are the framework's for every value; a handler adds the
/// rest for its field's value: what else changes when an item inside it is added, removed or moved, and the rules
/// between its parts. Rich Text is <c>{ markup, blocks }</c>: its handler places a block's tag in the markup when the
/// block is added, and requires every tag to have a block.
/// </summary>
public interface IValueHandler
{
    /// <summary>Told to the model in the field's schema: what else happens when the value is edited.</summary>
    string Usage { get; }

    /// <summary>An item was added to the items field at <paramref name="itemsPath"/> inside <paramref name="value"/>.</summary>
    void ItemAdded(JsonObject value, string itemsPath, Guid key)
    {
    }

    void ItemRemoved(JsonObject value, string itemsPath, Guid key)
    {
    }

    void ItemMoved(JsonObject value, string itemsPath, Guid key)
    {
    }

    /// <summary>Problems in the value, with paths inside it, e.g. a block tag in the markup with no block.</summary>
    IEnumerable<Problem> Validate(JsonObject value) => [];
}

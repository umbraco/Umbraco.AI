using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// The kind of operation a caller is asking the property value dispatcher to perform.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AIPropertyOperation>))]
public enum AIPropertyOperation
{
    /// <summary>
    /// Adds a new item to a collection-shaped property value.
    /// </summary>
    AddItem,

    /// <summary>
    /// Removes an item from a collection-shaped property value by its key.
    /// </summary>
    RemoveItem,

    /// <summary>
    /// Moves an item within a collection-shaped property value to a new position.
    /// </summary>
    MoveItem,

    /// <summary>
    /// Sets a scalar property value (or replaces the entire value of a property).
    /// </summary>
    SetValue,

    /// <summary>
    /// Clears a property value (sets to an empty/null state appropriate for the editor).
    /// </summary>
    ClearValue,
}

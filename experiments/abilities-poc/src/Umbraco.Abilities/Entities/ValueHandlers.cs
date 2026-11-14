using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// The owner of a value's inner model. A field's value is opaque to the framework unless its value type has a handler:
/// a property editor's (Rich Text's markup and blocks, a Block Grid's layout) or a product's own. The handler defines
/// the paths inside the value, what setting, adding, removing and moving mean there, and the rules between its parts.
/// The framework routes any path that goes into the field to it. Without a handler a value is set whole and checked
/// against its schema.
/// <para>
/// Keyed items (<see cref="FieldType.Items"/>) are the framework's own built-in value type, for products with fixed
/// models (discount rules, workflow steps); they keep per-item rebasing and reference checks. A value with a handler
/// rebases per field: finer merging is a capability a handler could add later.
/// </para>
/// </summary>
public interface IValueHandler
{
    /// <summary>Told to the model in the field's schema: what the inner paths are and what can be done there.</summary>
    string Usage { get; }

    /// <summary>Sets the value at a path inside <paramref name="value"/>, e.g. <c>markup</c> or <c>blocks/{key}/heading</c>.</summary>
    JsonNode? SetValue(JsonNode? value, string innerPath, JsonNode? newValue);

    /// <summary>Adds an item at a path inside the value. Returns the new value and the item's path inside it.</summary>
    ValueItem AddItem(JsonNode? value, string innerPath, string itemType, JsonObject? fields, Guid key, int? position);

    JsonNode? RemoveItem(JsonNode? value, string innerPath, Guid key);

    JsonNode? MoveItem(JsonNode? value, string innerPath, Guid key, int position);

    /// <summary>Problems in the value, with paths inside it, e.g. a block tag in the markup with no block.</summary>
    IEnumerable<Problem> Validate(JsonNode? value);
}

public sealed record ValueItem(JsonNode Value, string ItemPath);

/// <summary>A handler that supports only some operations: the rest refuse with a correctable message.</summary>
public abstract class ValueHandlerBase : IValueHandler
{
    public abstract string Usage { get; }

    public virtual JsonNode? SetValue(JsonNode? value, string innerPath, JsonNode? newValue) => throw NotSupported("Setting a value inside");

    public virtual ValueItem AddItem(JsonNode? value, string innerPath, string itemType, JsonObject? fields, Guid key, int? position) => throw NotSupported("Adding an item to");

    public virtual JsonNode? RemoveItem(JsonNode? value, string innerPath, Guid key) => throw NotSupported("Removing an item from");

    public virtual JsonNode? MoveItem(JsonNode? value, string innerPath, Guid key, int position) => throw NotSupported("Moving an item in");

    public virtual IEnumerable<Problem> Validate(JsonNode? value) => [];

    protected static AbilityInputException NotSupported(string what) => new($"{what} this value isn't supported. Set the whole value instead.");

    protected static AbilityInputException Invalid(string path, string message) => new(message, [new Problem(path, message)]);
}

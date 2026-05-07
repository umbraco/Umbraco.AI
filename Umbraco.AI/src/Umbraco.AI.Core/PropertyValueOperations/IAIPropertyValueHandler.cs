using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Implements the structural operations (add/remove/move/clear/get/set) for values produced by a
/// specific Umbraco property editor.
/// </summary>
/// <remarks>
/// <para>
/// Handlers are pure transformations from <c>(value, args, ctx)</c> to <c>newValue</c>. They do not
/// read or write to the database, do not know about workspace state, and do not reach back into
/// CMS services beyond the schema and default-value abstractions exposed via
/// <see cref="AIPropertyValueOperationContext"/>.
/// </para>
/// <para>
/// Handlers are auto-discovered via <see cref="IDiscoverable"/>, registered through
/// <c>builder.AIPropertyValueHandlers()</c>, and resolved by editor schema alias
/// (<see cref="ForPropertyEditorSchemaAlias"/>). Third parties register handlers for their own
/// editors to gain AI authoring capability with no further plumbing.
/// </para>
/// <para>
/// All operations work over <see cref="JsonNode"/> values and return new <see cref="JsonNode"/>
/// values; handlers must avoid mutating the input. The interface is the public C# plugin surface.
/// </para>
/// </remarks>
public interface IAIPropertyValueHandler : IDiscoverable
{
    /// <summary>
    /// Gets the property editor schema alias this handler operates on (e.g.
    /// <c>Umbraco.BlockList</c>).
    /// </summary>
    string ForPropertyEditorSchemaAlias { get; }

    /// <summary>
    /// Adds a new item to a collection-shaped value.
    /// </summary>
    /// <param name="value">The current value of the collection property.</param>
    /// <param name="args">The add-item arguments.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new value plus the freshly-minted block key.</returns>
    Task<AIAddItemHandlerResult> AddItemAsync(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the item with the given key from a collection-shaped value.
    /// </summary>
    Task<JsonNode?> RemoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the item with the given key to a new position in a collection-shaped value.
    /// </summary>
    Task<JsonNode?> MoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        int newPosition,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the value to the editor's empty representation (e.g. an empty block envelope, an
    /// empty picker array, an empty string).
    /// </summary>
    Task<JsonNode?> ClearAsync(
        JsonNode? value,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the content type key of an item within the collection. Used by the dispatcher to
    /// resolve the editor schema alias of a property nested inside the item before descending.
    /// </summary>
    /// <param name="value">The current value of the collection property.</param>
    /// <param name="blockKey">The item's block key.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>
    /// The item's content type key, or <c>null</c> if this editor's items do not have a content
    /// type (e.g. picker references) or the item is not present.
    /// </returns>
    Guid? GetItemContentTypeKey(
        JsonNode? value,
        Guid blockKey,
        AIPropertyValueOperationContext context);

    /// <summary>
    /// Returns the value of a property on the item with the given key. Used by the dispatcher to
    /// descend into nested values.
    /// </summary>
    /// <param name="value">The current value of the collection property.</param>
    /// <param name="blockKey">The item's block key.</param>
    /// <param name="propertyAlias">The alias of the property within the item to read.</param>
    /// <param name="variantId">Optional variant identifier; <c>null</c> uses the active variant.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>
    /// The property value as a <see cref="JsonNode"/>, or <c>null</c> if the item or property does
    /// not exist (or the property has no value for the requested variant).
    /// </returns>
    JsonNode? GetItemPropertyValue(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context);

    /// <summary>
    /// Sets the value of a property on the item with the given key. Used by the dispatcher to
    /// ascend after a leaf operation.
    /// </summary>
    Task<JsonNode?> SetItemPropertyValueAsync(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        JsonNode? newPropertyValue,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs handler-specific validation before any mutation. Default implementation accepts
    /// all input.
    /// </summary>
    /// <param name="value">The current value of the collection property.</param>
    /// <param name="args">The add-item arguments to validate.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>A validation result; <see cref="AIValidationResult.Valid"/> when the input is acceptable.</returns>
    AIValidationResult ValidateAddItem(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context) => AIValidationResult.Valid;
}

/// <summary>
/// Result of a successful <see cref="IAIPropertyValueHandler.AddItemAsync"/>.
/// </summary>
/// <param name="Value">The new collection value with the added item.</param>
/// <param name="BlockKey">The key of the added item.</param>
public sealed record AIAddItemHandlerResult(JsonNode? Value, Guid BlockKey);

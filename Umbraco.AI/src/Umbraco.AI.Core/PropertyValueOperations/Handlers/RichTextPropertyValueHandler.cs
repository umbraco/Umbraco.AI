using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.RichText</c> editor (Tiptap RTE with embedded blocks).
/// </summary>
/// <remarks>
/// <para>
/// The RTE value shape is:
/// <code>
/// { "markup": "&lt;html&gt;...", "blocks": { "layout": {...}, "contentData": [...], ... } }
/// </code>
/// Embedded blocks live in <c>value.blocks</c> and are referenced from inline placeholders inside
/// <c>markup</c>. Adding a block via this handler without inserting a corresponding markup
/// placeholder produces an orphan, so we reject <c>AddItem</c> outright; callers must edit the
/// markup directly via <c>set_value</c> to insert a placeholder, then use this handler's other
/// operations to set its property values.
/// </para>
/// <para>
/// All other operations work on the inner <c>blocks</c> envelope: remove/move walk the inner
/// layout, get/set property values target inner contentData entries.
/// </para>
/// </remarks>
public sealed class RichTextPropertyValueHandler : IAIPropertyValueHandler
{
    private const string EditorSchemaAlias = "Umbraco.RichText";
    private const string InnerBlocksLayoutKey = "Umbraco.RichText.Blocks";
    private const string MarkupPropertyName = "markup";
    private const string BlocksPropertyName = "blocks";

    private readonly IContentTypeService _contentTypeService;

    /// <summary>Initializes a new <see cref="RichTextPropertyValueHandler"/>.</summary>
    public RichTextPropertyValueHandler(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => EditorSchemaAlias;

    /// <inheritdoc />
    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
        => AIValidationResult.Invalid(new AIPropertyValueOperationError(
            AIPropertyValueOperationError.Codes.OperationNotSupported,
            "Cannot add a block to a rich-text property: blocks are anchored by markup placeholders. " +
            "Edit the markup via set_value to insert a placeholder, then use set_value with a path " +
            "into the embedded block to populate its properties."));

    /// <inheritdoc />
    public Task<AIAddItemHandlerResult> AddItemAsync(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("AddItem is not supported on RichText; ValidateAddItem must reject before this is reached.");

    /// <inheritdoc />
    public Task<JsonNode?> RemoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject rte)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var rteClone = (JsonObject)rte.DeepClone();
        var blocks = EnsureInnerBlocks(rteClone);
        BlockEnvelopeOps.RemoveByContentKey(blocks, InnerBlocksLayoutKey, blockKey);
        return Task.FromResult<JsonNode?>(rteClone);
    }

    /// <inheritdoc />
    public Task<JsonNode?> MoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        int newPosition,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject rte)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var rteClone = (JsonObject)rte.DeepClone();
        var blocks = EnsureInnerBlocks(rteClone);
        BlockEnvelopeOps.MoveInLayout(blocks, InnerBlocksLayoutKey, blockKey, newPosition);
        return Task.FromResult<JsonNode?>(rteClone);
    }

    /// <inheritdoc />
    public Task<JsonNode?> ClearAsync(
        JsonNode? value,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var empty = new JsonObject
        {
            [MarkupPropertyName] = string.Empty,
            [BlocksPropertyName] = BlockEnvelopeOps.Empty(InnerBlocksLayoutKey),
        };
        return Task.FromResult<JsonNode?>(empty);
    }

    /// <inheritdoc />
    public Guid? GetItemContentTypeKey(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context)
    {
        if (value is not JsonObject rte || rte[BlocksPropertyName] is not JsonObject blocks)
        {
            return null;
        }

        return BlockEnvelopeOps.FindContentTypeKey(blocks, blockKey);
    }

    /// <inheritdoc />
    public JsonNode? GetItemPropertyValue(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context)
    {
        if (value is not JsonObject rte || rte[BlocksPropertyName] is not JsonObject blocks)
        {
            return null;
        }

        var entry = BlockEnvelopeOps.FindContentDataEntry(blocks, blockKey);
        return entry is null ? null : BlockEnvelopeOps.GetPropertyValue(entry, propertyAlias, variantId);
    }

    /// <inheritdoc />
    public Task<JsonNode?> SetItemPropertyValueAsync(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        JsonNode? newPropertyValue,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject rte)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var rteClone = (JsonObject)rte.DeepClone();
        var blocks = EnsureInnerBlocks(rteClone);

        var entry = BlockEnvelopeOps.FindContentDataEntry(blocks, blockKey);
        if (entry is null)
        {
            return Task.FromResult<JsonNode?>(rteClone);
        }

        var contentTypeKey = BlockEnvelopeOps.FindContentTypeKey(blocks, blockKey);
        var editorAlias = contentTypeKey is null ? null : ResolvePropertyEditorAlias(contentTypeKey.Value, propertyAlias);

        BlockEnvelopeOps.SetPropertyValue(entry, propertyAlias, newPropertyValue, variantId, editorAlias);
        return Task.FromResult<JsonNode?>(rteClone);
    }

    private static JsonObject EnsureInnerBlocks(JsonObject rte)
    {
        if (rte[BlocksPropertyName] is not JsonObject blocks)
        {
            blocks = BlockEnvelopeOps.Empty(InnerBlocksLayoutKey);
            rte[BlocksPropertyName] = blocks;
        }

        return blocks;
    }

    private string? ResolvePropertyEditorAlias(Guid contentTypeKey, string propertyAlias)
    {
        var contentType = _contentTypeService.Get(contentTypeKey);
        var property = contentType?.CompositionPropertyTypes
            .FirstOrDefault(p => string.Equals(p.Alias, propertyAlias, StringComparison.OrdinalIgnoreCase));
        return property?.PropertyEditorAlias;
    }
}

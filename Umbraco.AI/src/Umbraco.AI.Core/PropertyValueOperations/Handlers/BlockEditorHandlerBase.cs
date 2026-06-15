using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Shared base for handlers operating on the canonical block envelope shape (block-list /
/// block-grid). Subclasses supply the editor's <see cref="LayoutKey"/> and
/// <see cref="BuildLayoutEntry"/>; everything else — content/settings data, expose,
/// remove/move/clear, item-property accessors, content-type and editor-alias resolution — lives
/// here in one place.
/// </summary>
/// <remarks>
/// Public so third-party handlers for custom block-shaped editors can inherit from it directly
/// rather than reimplementing the canonical envelope contract.
/// </remarks>
public abstract class BlockEditorHandlerBase : IAIPropertyValueHandler
{
    private readonly IContentTypeService _contentTypeService;

    /// <summary>Initializes a new <see cref="BlockEditorHandlerBase"/>.</summary>
    /// <param name="contentTypeService">CMS content type service used to resolve element-type aliases and property editor metadata.</param>
    protected BlockEditorHandlerBase(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <inheritdoc />
    public abstract string ForPropertyEditorSchemaAlias { get; }

    /// <summary>The key under <c>value.layout</c> that this editor uses for its layout array.</summary>
    protected abstract string LayoutKey { get; }

    /// <summary>
    /// Builds the editor-specific layout entry. Block-list emits <c>{ contentKey, settingsKey? }</c>;
    /// block-grid emits the same plus <c>areas</c> / <c>columnSpan</c> / <c>rowSpan</c>.
    /// </summary>
    protected abstract JsonObject BuildLayoutEntry(Guid contentKey, Guid? settingsKey, AIAddItemArgs args);

    /// <inheritdoc />
    public virtual AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
        => AIValidationResult.Valid;

    /// <inheritdoc />
    public Task<AIAddItemHandlerResult> AddItemAsync(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var envelope = BlockEnvelopeOps.AsEnvelope(value, LayoutKey);
        var variant = args.Variant ?? PrimaryVariant(context);

        var contentTypeKey = ResolveContentTypeKey(args.ElementType);
        var contentKey = BlockEnvelopeOps.AddContentDataEntry(
            envelope,
            contentTypeKey,
            BuildValuesArray(args.Values, variant));

        Guid? settingsKey = null;
        if (args.SettingsValues is not null)
        {
            // Settings element type isn't carried on AIAddItemArgs today; reuse the content type
            // as a placeholder until we surface it explicitly.
            settingsKey = BlockEnvelopeOps.AddSettingsDataEntry(
                envelope,
                contentTypeKey,
                BuildValuesArray(args.SettingsValues, variant));
        }

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, BuildLayoutEntry(contentKey, settingsKey, args), args.Position);

        var exposeArray = BlockEnvelopeOps.GetOrCreateArray(envelope, BlockEnvelopeOps.ExposePropertyName);
        foreach (var entry in ExposeBuilder.Build(contentKey, context.DocumentMetadata.Variants))
        {
            exposeArray.Add(entry);
        }

        return Task.FromResult(new AIAddItemHandlerResult(envelope, contentKey));
    }

    /// <inheritdoc />
    public Task<JsonNode?> RemoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject obj)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var envelope = (JsonObject)obj.DeepClone();
        BlockEnvelopeOps.RemoveByContentKey(envelope, LayoutKey, blockKey);
        return Task.FromResult<JsonNode?>(envelope);
    }

    /// <inheritdoc />
    public Task<JsonNode?> MoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        int newPosition,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject obj)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var envelope = (JsonObject)obj.DeepClone();
        BlockEnvelopeOps.MoveInLayout(envelope, LayoutKey, blockKey, newPosition);
        return Task.FromResult<JsonNode?>(envelope);
    }

    /// <inheritdoc />
    public Task<JsonNode?> ClearAsync(
        JsonNode? value,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult<JsonNode?>(BlockEnvelopeOps.Empty(LayoutKey));

    /// <inheritdoc />
    public Guid? GetItemContentTypeKey(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context)
        => value is JsonObject obj ? BlockEnvelopeOps.FindContentTypeKey(obj, blockKey) : null;

    /// <inheritdoc />
    public JsonNode? GetItemPropertyValue(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context)
    {
        if (value is not JsonObject obj)
        {
            return null;
        }

        var entry = BlockEnvelopeOps.FindContentDataEntry(obj, blockKey);
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
        if (value is not JsonObject obj)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var envelope = (JsonObject)obj.DeepClone();
        var entry = BlockEnvelopeOps.FindContentDataEntry(envelope, blockKey);
        if (entry is null)
        {
            return Task.FromResult<JsonNode?>(envelope);
        }

        var contentTypeKey = BlockEnvelopeOps.FindContentTypeKey(envelope, blockKey);
        var editorAlias = contentTypeKey is null ? null : ResolvePropertyEditorAlias(contentTypeKey.Value, propertyAlias);

        BlockEnvelopeOps.SetPropertyValue(entry, propertyAlias, newPropertyValue, variantId, editorAlias);
        return Task.FromResult<JsonNode?>(envelope);
    }

    private static AIVariantId? PrimaryVariant(AIPropertyValueOperationContext context)
        => context.DocumentMetadata.Variants.Count > 0 ? context.DocumentMetadata.Variants[0] : null;

    private Guid ResolveContentTypeKey(string? elementType)
    {
        if (string.IsNullOrWhiteSpace(elementType))
        {
            return Guid.Empty;
        }

        if (Guid.TryParse(elementType, out var key))
        {
            return key;
        }

        var byAlias = _contentTypeService.Get(elementType);
        return byAlias?.Key ?? Guid.Empty;
    }

    private string? ResolvePropertyEditorAlias(Guid contentTypeKey, string propertyAlias)
    {
        var contentType = _contentTypeService.Get(contentTypeKey);
        var property = contentType?.CompositionPropertyTypes
            .FirstOrDefault(p => string.Equals(p.Alias, propertyAlias, StringComparison.OrdinalIgnoreCase));
        return property?.PropertyEditorAlias;
    }

    private static JsonArray BuildValuesArray(JsonObject? values, AIVariantId? variant)
    {
        var array = new JsonArray();
        if (values is null)
        {
            return array;
        }

        foreach (var (alias, node) in values)
        {
            array.Add(new JsonObject
            {
                ["alias"] = alias,
                ["culture"] = variant?.Culture,
                ["segment"] = variant?.Segment,
                ["value"] = node?.DeepClone(),
            });
        }

        return array;
    }
}

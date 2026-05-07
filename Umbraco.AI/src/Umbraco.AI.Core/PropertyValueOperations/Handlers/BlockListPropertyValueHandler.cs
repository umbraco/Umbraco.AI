using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.BlockList</c> editor.
/// </summary>
/// <remarks>
/// Operates on the canonical block list envelope:
/// <code>
/// {
///   "layout": { "Umbraco.BlockList": [ { "contentKey": "...", "settingsKey": "..." }, ... ] },
///   "contentData": [ ... ],
///   "settingsData": [ ... ],
///   "expose": [ ... ]
/// }
/// </code>
/// </remarks>
public sealed class BlockListPropertyValueHandler : IAIPropertyValueHandler
{
    private const string LayoutKey = "Umbraco.BlockList";

    private readonly IContentTypeService _contentTypeService;

    /// <summary>Initializes a new <see cref="BlockListPropertyValueHandler"/>.</summary>
    public BlockListPropertyValueHandler(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => LayoutKey;

    /// <inheritdoc />
    public Task<AIAddItemHandlerResult> AddItemAsync(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var envelope = BlockEnvelopeOps.AsEnvelope(value, LayoutKey);

        var contentTypeKey = ResolveContentTypeKey(args.ElementType);
        var contentKey = BlockEnvelopeOps.AddContentDataEntry(
            envelope,
            contentTypeKey,
            BuildValuesArray(args.Values, args.Variant ?? PrimaryVariant(context)),
            key: null);

        Guid? settingsKey = null;
        if (args.SettingsValues is not null)
        {
            // Settings element type isn't in args today; use the same content type as a placeholder
            // — handlers can refine when CMS exposes the per-block settings element type to us.
            settingsKey = BlockEnvelopeOps.AddSettingsDataEntry(
                envelope,
                contentTypeKey,
                BuildValuesArray(args.SettingsValues, args.Variant ?? PrimaryVariant(context)));
        }

        // Build the layout entry.
        var layoutEntry = new JsonObject { ["contentKey"] = contentKey };
        if (settingsKey is not null)
        {
            layoutEntry["settingsKey"] = settingsKey;
        }

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, layoutEntry, args.Position);

        // Add expose entries for the supplied variants (or invariant when none supplied).
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
        return entry is null
            ? null
            : BlockEnvelopeOps.GetPropertyValue(entry, propertyAlias, variantId);
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

        // Resolve editor alias by looking up the property on the block's content type.
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

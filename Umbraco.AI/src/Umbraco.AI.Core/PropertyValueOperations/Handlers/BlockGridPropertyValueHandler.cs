using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.BlockGrid</c> editor.
/// </summary>
/// <remarks>
/// <para>
/// v1 supports root-level operations only: <c>AddItem</c> appends/inserts at the top level,
/// <c>RemoveItem</c> and <c>MoveItem</c> work on the root layout array. Edits inside rows, areas,
/// or columns are explicitly rejected via <see cref="ValidateAddItem"/> when the caller supplies a
/// non-empty <c>gridArea</c>. The reserved parameter shape lets v2 fill in row/area/span support
/// without an API break.
/// </para>
/// <para>
/// Property edits inside an existing block (<see cref="SetItemPropertyValueAsync"/>) work
/// identically to block-list — they mutate <c>contentData</c>, never the layout.
/// </para>
/// </remarks>
public sealed class BlockGridPropertyValueHandler : IAIPropertyValueHandler
{
    private const string LayoutKey = "Umbraco.BlockGrid";

    private readonly IContentTypeService _contentTypeService;

    /// <summary>Initializes a new <see cref="BlockGridPropertyValueHandler"/>.</summary>
    public BlockGridPropertyValueHandler(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => LayoutKey;

    /// <inheritdoc />
    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
    {
        // v1 only supports root-level inserts. Block-grid layout entries can specify a row + area
        // + columnSpan/rowSpan; supplying any of these means the caller wants to nest into a row,
        // which we explicitly reject until v2.
        if (args.Extra is not null && args.Extra.Count > 0)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.OperationNotSupported,
                "Block-grid v1 supports only root-level adds. Row/area/span placement is not yet supported.",
                Details: new JsonObject { ["unsupportedFields"] = new JsonArray(args.Extra.Select(kvp => (JsonNode?)kvp.Key).ToArray()) }));
        }

        return AIValidationResult.Valid;
    }

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
            BuildValuesArray(args.Values, args.Variant ?? PrimaryVariant(context)));

        Guid? settingsKey = null;
        if (args.SettingsValues is not null)
        {
            settingsKey = BlockEnvelopeOps.AddSettingsDataEntry(
                envelope,
                contentTypeKey,
                BuildValuesArray(args.SettingsValues, args.Variant ?? PrimaryVariant(context)));
        }

        // Root-level layout entry. No areas/spans in v1; CMS infers a sensible default span.
        var layoutEntry = new JsonObject
        {
            ["contentKey"] = contentKey,
            ["areas"] = new JsonArray(),
            ["columnSpan"] = 12,
            ["rowSpan"] = 1,
        };
        if (settingsKey is not null)
        {
            layoutEntry["settingsKey"] = settingsKey;
        }

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, layoutEntry, args.Position);

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

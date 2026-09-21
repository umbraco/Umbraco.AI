using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.BlockGrid</c> editor.
/// </summary>
/// <remarks>
/// <para>
/// v1 supports root-level operations only. Property edits inside an existing block
/// (<see cref="BlockEditorHandlerBase.SetItemPropertyValueAsync"/>) work identically to
/// block-list — they mutate <c>contentData</c>, never the layout. Edits inside rows/areas/columns
/// are explicitly rejected via <see cref="ValidateAddItem"/> when the caller supplies anything in
/// <see cref="AIAddItemArgs.Extra"/>; the reserved parameter shape lets v2 fill in row/area/span
/// support without an API break.
/// </para>
/// </remarks>
public sealed class BlockGridPropertyValueHandler : BlockEditorHandlerBase
{
    /// <summary>Initializes a new <see cref="BlockGridPropertyValueHandler"/>.</summary>
    public BlockGridPropertyValueHandler(IContentTypeService contentTypeService)
        : base(contentTypeService)
    {
    }

    /// <inheritdoc />
    public override string ForPropertyEditorSchemaAlias => "Umbraco.BlockGrid";

    /// <inheritdoc />
    protected override string LayoutKey => "Umbraco.BlockGrid";

    /// <inheritdoc />
    public override AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
    {
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
    /// <remarks>
    /// v1 can only rewrite the root layout array, so it cannot correctly remove a block nested
    /// inside another block's area: the layout entry lives out of reach (inside the parent's
    /// <c>areas</c>), but <c>contentData</c> is keyed flat, so a naive removal would delete the
    /// nested block's content while leaving its layout entry (and settings) behind. Reject the key
    /// instead of partially applying the removal.
    /// </remarks>
    public override AIValidationResult ValidateRemoveItem(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context)
    {
        if (value is JsonObject envelope && IsNestedInArea(envelope, blockKey))
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.OperationNotSupported,
                $"Block '{blockKey}' is nested inside a block-grid area. Block-grid v1 supports only root-level removal.",
                Details: new JsonObject { ["blockKey"] = blockKey.ToString() }));
        }

        return AIValidationResult.Valid;
    }

    private bool IsNestedInArea(JsonObject envelope, Guid contentKey)
    {
        if (envelope[BlockEnvelopeOps.LayoutPropertyName] is not JsonObject layoutObj ||
            layoutObj[LayoutKey] is not JsonArray rootLayoutArray)
        {
            return false;
        }

        foreach (var rootEntry in rootLayoutArray)
        {
            if (rootEntry is JsonObject rootObj && ContainsNestedContentKey(rootObj, contentKey))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNestedContentKey(JsonObject layoutItem, Guid contentKey)
    {
        if (layoutItem["areas"] is not JsonArray areas)
        {
            return false;
        }

        foreach (var area in areas)
        {
            if (area is not JsonObject areaObj || areaObj["items"] is not JsonArray items)
            {
                continue;
            }

            foreach (var item in items)
            {
                if (item is not JsonObject itemObj)
                {
                    continue;
                }

                if (BlockEnvelopeOps.GetGuid(itemObj, BlockEnvelopeOps.ContentKeyPropertyName) == contentKey)
                {
                    return true;
                }

                if (ContainsNestedContentKey(itemObj, contentKey))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override JsonObject BuildLayoutEntry(Guid contentKey, Guid? settingsKey, AIAddItemArgs args)
    {
        // v1 emits root-level entries with no areas and a sensible default span.
        var entry = new JsonObject
        {
            ["contentKey"] = contentKey,
            ["areas"] = new JsonArray(),
            ["columnSpan"] = 12,
            ["rowSpan"] = 1,
        };
        if (settingsKey is not null)
        {
            entry["settingsKey"] = settingsKey;
        }
        return entry;
    }
}

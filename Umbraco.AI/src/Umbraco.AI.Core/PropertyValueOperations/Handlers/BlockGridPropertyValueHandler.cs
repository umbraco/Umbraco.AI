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

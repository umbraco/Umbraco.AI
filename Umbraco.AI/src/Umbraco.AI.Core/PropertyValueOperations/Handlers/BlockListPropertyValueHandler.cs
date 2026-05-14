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
public sealed class BlockListPropertyValueHandler : BlockEditorHandlerBase
{
    /// <summary>Initializes a new <see cref="BlockListPropertyValueHandler"/>.</summary>
    public BlockListPropertyValueHandler(IContentTypeService contentTypeService)
        : base(contentTypeService)
    {
    }

    /// <inheritdoc />
    public override string ForPropertyEditorSchemaAlias => "Umbraco.BlockList";

    /// <inheritdoc />
    protected override string LayoutKey => "Umbraco.BlockList";

    /// <inheritdoc />
    protected override JsonObject BuildLayoutEntry(Guid contentKey, Guid? settingsKey, AIAddItemArgs args)
    {
        var entry = new JsonObject { ["contentKey"] = contentKey };
        if (settingsKey is not null)
        {
            entry["settingsKey"] = settingsKey;
        }
        return entry;
    }
}

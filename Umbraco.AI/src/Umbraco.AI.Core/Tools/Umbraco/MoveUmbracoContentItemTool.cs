using System.ComponentModel;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the MoveUmbracoContentItem tool.
/// </summary>
public record MoveUmbracoContentItemArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Path identifying the collection-shaped property (Block List, Block Grid, etc.) the item lives in, potentially nested inside another block. Must start and end with an alias segment.")]
    IReadOnlyList<UmbracoPropertyPathSegmentArg> Path,

    [property: Description("The key of the block/item to move, as returned by add_umbraco_content_item or seen in the property's current value.")]
    Guid BlockKey,

    [property: Description("The new zero-based position for the item within the collection.")]
    int Position,

    [property: Description("Optional culture code (e.g., 'en-US') when the content item varies by culture.")]
    string? Culture = null,

    [property: Description("Optional segment identifier when the content item is segmented.")]
    string? Segment = null);

/// <summary>
/// Tool that moves an item to a new position within a collection-shaped content property (Block List,
/// Block Grid, etc.).
/// </summary>
[AITool("move_umbraco_content_item", "Move Umbraco Content Item", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class MoveUmbracoContentItemTool(
    IContentEditingService contentEditingService,
    IAIPropertyValueDispatcher dispatcher,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<MoveUmbracoContentItemArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Moves an item to a new position within a collection-shaped content property (Block List, Block " +
        "Grid, etc.). Persists immediately as a draft — call publish_umbraco_content afterward to make the change live.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(MoveUmbracoContentItemArgs args, CancellationToken cancellationToken = default)
    {
        var dispatchArgs = new JsonObject
        {
            ["blockKey"] = args.BlockKey.ToString(),
            ["position"] = args.Position,
        };

        var outcome = await ContentPropertyValueOperationHelper.ExecuteAsync(
            authorizer,
            contentEditingService,
            dispatcher,
            args.Key,
            args.Path,
            AIPropertyOperation.MoveItem,
            dispatchArgs,
            args.Culture,
            args.Segment,
            cancellationToken);

        return new MoveUmbracoContentItemResult(outcome.Success, outcome.Message);
    }
}

/// <summary>
/// Result of the move Umbraco content item tool.
/// </summary>
public record MoveUmbracoContentItemResult(
    bool Success,
    string? Message);

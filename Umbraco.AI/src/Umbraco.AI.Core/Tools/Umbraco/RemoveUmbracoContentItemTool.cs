using System.ComponentModel;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the RemoveUmbracoContentItem tool.
/// </summary>
public record RemoveUmbracoContentItemArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Path identifying the collection-shaped property (Block List, Block Grid, etc.) the item lives in, potentially nested inside another block. Must start and end with an alias segment.")]
    IReadOnlyList<UmbracoPropertyPathSegmentArg> Path,

    [property: Description("The key of the block/item to remove, as returned by add_umbraco_content_item or seen in the property's current value.")]
    Guid BlockKey,

    [property: Description("Optional culture code (e.g., 'en-US') when the content item varies by culture.")]
    string? Culture = null,

    [property: Description("Optional segment identifier when the content item is segmented.")]
    string? Segment = null);

/// <summary>
/// Tool that removes an item from a collection-shaped content property (Block List, Block Grid, etc.)
/// by its key.
/// </summary>
[AITool("remove_umbraco_content_item", "Remove Umbraco Content Item", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class RemoveUmbracoContentItemTool(
    IContentEditingService contentEditingService,
    IAIPropertyValueDispatcher dispatcher,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<RemoveUmbracoContentItemArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Removes an item from a collection-shaped content property (Block List, Block Grid, etc.) by its " +
        "key. Persists immediately as a draft — call publish_umbraco_content afterward to make the change live.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(RemoveUmbracoContentItemArgs args, CancellationToken cancellationToken = default)
    {
        var dispatchArgs = new JsonObject { ["blockKey"] = args.BlockKey.ToString() };

        var outcome = await ContentPropertyValueOperationHelper.ExecuteAsync(
            authorizer,
            contentEditingService,
            dispatcher,
            args.Key,
            args.Path,
            AIPropertyOperation.RemoveItem,
            dispatchArgs,
            args.Culture,
            args.Segment,
            cancellationToken);

        return new RemoveUmbracoContentItemResult(outcome.Success, outcome.Message);
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(RemoveUmbracoContentItemArgs args)
    {
        var propertyAlias = args.Path.LastOrDefault()?.Alias;
        return propertyAlias is null ? null : $"Remove an item from '{propertyAlias}'.";
    }
}

/// <summary>
/// Result of the remove Umbraco content item tool.
/// </summary>
public record RemoveUmbracoContentItemResult(
    bool Success,
    string? Message);

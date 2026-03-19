using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetContentTreePath tool.
/// </summary>
/// <param name="ContentKey">The unique key of the content item.</param>
/// <param name="IncludeSiblings">Whether to include sibling content items at the same level.</param>
public record GetContentTreePathArgs(
    [property: Description("The unique key (GUID) of the content item to get the tree path for.")]
    Guid ContentKey,

    [property: Description("Whether to include sibling items at the same level (default false). Useful for understanding what other pages exist alongside this one.")]
    bool? IncludeSiblings = false);

/// <summary>
/// Tool that retrieves the ancestor chain and optionally siblings for a content item,
/// providing full context about its position in the content tree.
/// </summary>
[AITool("get_content_tree_path", "Get Content Tree Path", ScopeId = ContentReadScope.ScopeId)]
public class GetContentTreePathTool : AIToolBase<GetContentTreePathArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTreePathTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public GetContentTreePathTool(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the full ancestor chain (from root to parent) for a content item. " +
        "Optionally includes siblings (other items at the same level). " +
        "Use this to understand a content item's position in the site hierarchy, " +
        "find related pages, or navigate the content tree structure.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetContentTreePathArgs args, CancellationToken cancellationToken = default)
    {
        if (args.ContentKey == Guid.Empty)
        {
            return Task.FromResult<object>(new GetContentTreePathResult(
                false, null, null, null, "Content key cannot be empty."));
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetContentTreePathResult(
                false, null, null, null, "Umbraco context is not available."));
        }

        var content = umbracoContext.Content?.GetById(args.ContentKey);
        if (content is null)
        {
            return Task.FromResult<object>(new GetContentTreePathResult(
                false, null, null, null, $"Content with key '{args.ContentKey}' was not found or is not published."));
        }

        // Build ancestor chain (root to parent)
        var ancestors = content.Ancestors()
            .Reverse()
            .Select(a => new ContentTreeNode(a.Key, a.Name, a.ContentType.Alias, a.Url(), a.Level))
            .ToList();

        var current = new ContentTreeNode(
            content.Key, content.Name, content.ContentType.Alias, content.Url(), content.Level);

        // Optionally get siblings (only when content has a parent)
        IReadOnlyList<ContentTreeNode>? siblings = null;
        if (args.IncludeSiblings == true)
        {
            var parent = content.Parent();
            if (parent != null)
            {
                var siblingItems = parent.Children() ?? [];
                siblings = siblingItems
                    .Where(s => s.Key != content.Key) // Exclude self
                    .Select(s => new ContentTreeNode(s.Key, s.Name, s.ContentType.Alias, s.Url(), s.Level))
                    .ToList();
            }
            else
            {
                siblings = []; // Root-level content has no siblings accessible via published cache
            }
        }

        return Task.FromResult<object>(new GetContentTreePathResult(
            true, ancestors, current, siblings, null));
    }
}

/// <summary>
/// Result of the get content tree path tool.
/// </summary>
/// <param name="Success">Whether the content was found.</param>
/// <param name="Ancestors">The ancestor chain from root to parent.</param>
/// <param name="Current">The current content item.</param>
/// <param name="Siblings">Siblings at the same level (if requested).</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record GetContentTreePathResult(
    bool Success,
    IReadOnlyList<ContentTreeNode>? Ancestors,
    ContentTreeNode? Current,
    IReadOnlyList<ContentTreeNode>? Siblings,
    string? Message);

/// <summary>
/// A node in the content tree.
/// </summary>
/// <param name="Key">The unique key.</param>
/// <param name="Name">The name.</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL.</param>
/// <param name="Level">The depth level in the tree.</param>
public record ContentTreeNode(
    Guid Key,
    string Name,
    string ContentType,
    string? Url,
    int Level);

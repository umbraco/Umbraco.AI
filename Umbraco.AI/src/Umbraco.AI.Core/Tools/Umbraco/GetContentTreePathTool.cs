using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.DependencyInjection;
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
    bool IncludeSiblings = false);

/// <summary>
/// Tool that retrieves the ancestor chain and optionally siblings for a content item,
/// providing full context about its position in the content tree.
/// </summary>
[AITool("get_content_tree_path", "Get Content Tree Path", ScopeId = ContentReadScope.ScopeId)]
public class GetContentTreePathTool : AIToolBase<GetContentTreePathArgs>
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IUmbracoReadAuthorizer _readAuthorizer;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTreePathTool"/>.
    /// </summary>
    /// <param name="umbracoContextFactory">The Umbraco context factory.</param>
    [Obsolete("Use the constructor that accepts an IUmbracoReadAuthorizer. Will be removed in v20.")]
    public GetContentTreePathTool(IUmbracoContextFactory umbracoContextFactory)
        : this(umbracoContextFactory, StaticServiceProvider.Instance.GetRequiredService<IUmbracoReadAuthorizer>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTreePathTool"/>.
    /// </summary>
    /// <param name="umbracoContextFactory">The Umbraco context factory.</param>
    /// <param name="readAuthorizer">Limits results to what the acting user's content tree shows.</param>
    public GetContentTreePathTool(IUmbracoContextFactory umbracoContextFactory, IUmbracoReadAuthorizer readAuthorizer)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _readAuthorizer = readAuthorizer;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the full ancestor chain (from root to parent) for a content item. " +
        "Optionally includes siblings (other items at the same level). " +
        "Use this to understand a content item's position in the site hierarchy, " +
        "find related pages, or navigate the content tree structure.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetContentTreePathArgs args, CancellationToken cancellationToken = default)
    {
        if (args.ContentKey == Guid.Empty)
        {
            return new GetContentTreePathResult(
                false, null, null, null, "Content key cannot be empty.");
        }

        // Ensure UmbracoContext exists — not automatically created for backoffice API requests, and
        // never created at all when this tool is invoked from Umbraco.Automate's background dispatcher.
        // A no-op if a context is already ambient.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // An item the user's tree doesn't show is reported the same as not found. Every ancestor of an
        // item the tree shows is itself shown, so the chain below needs no further filtering.
        var content = contextReference.UmbracoContext.Content?.GetById(args.ContentKey);
        if (content is null || !await _readAuthorizer.CanSeeContentInTreeAsync(content.Key, content.Path))
        {
            return new GetContentTreePathResult(
                false, null, null, null, $"Content with key '{args.ContentKey}' was not found or is not published.");
        }

        // Build ancestor chain (root to parent)
        var ancestors = content.Ancestors()
            .Reverse()
            .Select(a => new ContentTreeNode(a.Key, a.Name, a.ContentType.Alias, a.Url(), a.Level))
            .ToList();

        var current = new ContentTreeNode(
            content.Key, content.Name, content.ContentType.Alias, content.Url(), content.Level);

        // Optionally get siblings using the navigation-backed Siblings() extension
        IReadOnlyList<ContentTreeNode>? siblings = null;
        if (args.IncludeSiblings)
        {
            var visibleSiblings = new List<ContentTreeNode>();
            foreach (var sibling in content.Siblings())
            {
                if (await _readAuthorizer.CanSeeContentInTreeAsync(sibling.Key, sibling.Path))
                {
                    visibleSiblings.Add(new ContentTreeNode(sibling.Key, sibling.Name, sibling.ContentType.Alias, sibling.Url(), sibling.Level));
                }
            }

            siblings = visibleSiblings;
        }

        return new GetContentTreePathResult(
            true, ancestors, current, siblings, null);
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

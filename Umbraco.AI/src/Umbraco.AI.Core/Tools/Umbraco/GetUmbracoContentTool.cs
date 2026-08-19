using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetUmbracoContent tool.
/// </summary>
/// <param name="Key">The unique key of the content item.</param>
/// <param name="Culture">Optional culture code for variant content.</param>
public record GetUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to retrieve. Use IDs from search_umbraco results.")]
    Guid Key,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that retrieves a content item from Umbraco by its key, including all property values. Reads
/// from the business/draft layer via <see cref="IContentEditingService"/> — the same layer the write
/// tools operate on — so it works for unpublished drafts as well as published content. Unlike the write
/// tools' own confirmation payloads, this enriches the result with a real breadcrumb, parent info, and
/// (when published) a live URL, since a lookup tool's caller usually doesn't already know those.
/// </summary>
[AITool("get_umbraco_content", "Get Umbraco Content", ScopeId = ContentReadScope.ScopeId)]
public class GetUmbracoContentTool(
    IContentEditingService contentEditingService,
    IContentService contentService,
    IUmbracoContextAccessor umbracoContextAccessor)
    : AIToolBase<GetUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Retrieves a content item from Umbraco by its unique key (GUID), whether it's published or still " +
        "an unpublished draft. Returns the full content including all property values, content type, " +
        "breadcrumb path, and parent info. When the item (and requested culture) is published, also " +
        "includes its live URL. Use IDs from search_umbraco results to fetch detailed content. For " +
        "variant (multilingual) content, specify the culture code to get culture-specific values.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new GetUmbracoContentResult(false, null, "Content key cannot be empty.");
        }

        var content = await contentEditingService.GetAsync(args.Key);
        if (content is null)
        {
            return new GetUmbracoContentResult(false, null, $"Content with key '{args.Key}' was not found.");
        }

        var item = ContentToolHelpers.BuildEnrichedContentItem(content, contentService, umbracoContextAccessor, args.Culture);

        return new GetUmbracoContentResult(true, item, null);
    }
}

/// <summary>
/// Result of the get Umbraco content tool.
/// </summary>
/// <param name="Success">Whether the content was found.</param>
/// <param name="Content">The content item, if found.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record GetUmbracoContentResult(
    bool Success,
    UmbracoContentItem? Content,
    string? Message);

/// <summary>
/// A content item with full property values. Shared across read and write tools, so its richness
/// depends on which <c>ContentToolHelpers</c> builder method built it: <c>BuildContentItem(IPublishedContent)</c>
/// (<c>get_content_by_route</c>, resource types) and <c>BuildEnrichedContentItem</c> (<c>get_umbraco_content</c>)
/// both resolve a friendly breadcrumb <see cref="Path"/> and <see cref="Parent"/> info, with <see cref="Url"/>
/// populated when the item is published; the lean <c>BuildContentItem(IContent)</c> used by write tools'
/// own confirmation payloads (<c>create_umbraco_content</c>, <c>update_umbraco_content</c>) — whose caller
/// already knows the parent it just passed in — always leaves <see cref="Url"/> and <see cref="Parent"/>
/// null and <see cref="Path"/> as the raw comma-separated id path.
/// </summary>
/// <param name="Key">The unique key of the content item.</param>
/// <param name="Name">The name of the content item.</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL of the content item, if it's published.</param>
/// <param name="CreateDate">The creation date.</param>
/// <param name="UpdateDate">The last update date.</param>
/// <param name="Level">The depth level in the content tree.</param>
/// <param name="Path">The breadcrumb path (e.g., "Home > About > Team") or the raw id path, depending on source.</param>
/// <param name="Parent">The parent content item info, if resolved.</param>
/// <param name="Properties">The content properties with their values.</param>
public record UmbracoContentItem(
    Guid Key,
    string Name,
    string ContentType,
    string? Url,
    DateTime CreateDate,
    DateTime UpdateDate,
    int Level,
    string Path,
    ContentParentItem? Parent,
    IReadOnlyList<ContentPropertyItem> Properties);

/// <summary>
/// Summary info about a content item's parent.
/// </summary>
/// <param name="Key">The parent's unique key.</param>
/// <param name="Name">The parent's name.</param>
public record ContentParentItem(Guid Key, string Name);

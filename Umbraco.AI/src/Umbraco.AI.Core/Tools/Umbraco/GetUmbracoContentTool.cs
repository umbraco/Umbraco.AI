using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

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
/// tools operate on — so it works for unpublished drafts as well as published content, and returns the
/// same <see cref="UmbracoContentItem"/> shape those tools do (raw stored property values, no resolved
/// URL, no friendly breadcrumb/parent).
/// </summary>
[AITool("get_umbraco_content", "Get Umbraco Content", ScopeId = ContentReadScope.ScopeId)]
public class GetUmbracoContentTool(IContentEditingService contentEditingService) : AIToolBase<GetUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Retrieves a content item from Umbraco by its unique key (GUID), whether it's published or still " +
        "an unpublished draft. Returns the full content including all property values, content type, and " +
        "metadata — the same shape create_umbraco_content/update_umbraco_content return. Use IDs from " +
        "search_umbraco results to fetch detailed content. For variant (multilingual) content, specify " +
        "the culture code to get culture-specific values.";

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

        var item = ContentToolHelpers.BuildContentItem(content, args.Culture);

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
/// depends on which <c>ContentToolHelpers.BuildContentItem</c> overload built it: from the published
/// cache (<c>get_content_by_route</c>, resource types) it carries a resolved <see cref="Url"/>, a
/// friendly breadcrumb <see cref="Path"/>, and <see cref="Parent"/> info; from the business/draft layer
/// (<c>get_umbraco_content</c>, <c>create_umbraco_content</c>, <c>update_umbraco_content</c>) — which
/// works for unpublished drafts too — <see cref="Url"/> and <see cref="Parent"/> are always null and
/// <see cref="Path"/> is the raw comma-separated id path.
/// </summary>
/// <param name="Key">The unique key of the content item.</param>
/// <param name="Name">The name of the content item.</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL of the content item, if resolved from the published cache.</param>
/// <param name="CreateDate">The creation date.</param>
/// <param name="UpdateDate">The last update date.</param>
/// <param name="Level">The depth level in the content tree.</param>
/// <param name="Path">The breadcrumb path (e.g., "Home > About > Team") or the raw id path, depending on source.</param>
/// <param name="Parent">The parent content item info, if resolved from the published cache.</param>
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

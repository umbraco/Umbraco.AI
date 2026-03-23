using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Shared helper methods for content tools.
/// </summary>
internal static class ContentToolHelpers
{
    /// <summary>
    /// Builds a breadcrumb path string for a content item (e.g., "Home > About > Team").
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <returns>A breadcrumb path string.</returns>
    public static string GetContentPath(IPublishedContent content)
    {
        var ancestors = content.Ancestors();
        var pathParts = ancestors.Reverse().Select(a => a.Name).ToList();
        pathParts.Add(content.Name);
        return string.Join(" > ", pathParts);
    }

    /// <summary>
    /// Builds an <see cref="UmbracoContentItem"/> from a published content item,
    /// extracting all properties and parent info.
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A fully populated content item.</returns>
    public static UmbracoContentItem BuildContentItem(IPublishedContent content, string? culture = null)
    {
        var properties = PropertyValueFormatter.ExtractProperties(content, culture);

        var parentInfo = content.Parent() is { } parent
            ? new ContentParentItem(parent.Key, parent.Name)
            : null;

        return new UmbracoContentItem(
            content.Key,
            content.Name,
            content.ContentType.Alias,
            content.Url(),
            content.CreateDate,
            content.UpdateDate,
            content.Level,
            GetContentPath(content),
            parentInfo,
            properties);
    }
}

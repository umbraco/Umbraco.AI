using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Shared helpers for Umbraco search tools.
/// </summary>
internal static class UmbracoSearchHelpers
{
    /// <summary>
    /// Builds a breadcrumb path string for the given content item (e.g., "Root > Parent > Child").
    /// </summary>
    internal static string GetContentPath(IPublishedContent content)
    {
        var pathParts = new List<string>();
        var current = content;

        while (current is not null)
        {
            pathParts.Add(current.Name);
            current = current.Parent();
        }

        pathParts.Reverse();
        return string.Join(" > ", pathParts);
    }

    /// <summary>
    /// Gets a thumbnail URL for media items, applying crop parameters for images.
    /// </summary>
    internal static string? GetMediaThumbnailUrl(IPublishedContent media)
    {
        if (media.ContentType.Alias.Contains("Image", StringComparison.OrdinalIgnoreCase))
        {
            var url = media.Url();
            if (!string.IsNullOrEmpty(url))
            {
                return $"{url}?width=200&height=200&mode=crop";
            }
        }

        return media.Url();
    }

    /// <summary>
    /// Creates an enriched result item from published content.
    /// </summary>
    internal static UmbracoSearchResultItem CreateEnrichedResultItem(IPublishedContent content, float score, bool isMedia)
    {
        var thumbnailUrl = isMedia ? GetMediaThumbnailUrl(content) : null;

        return new UmbracoSearchResultItem(
            content.Key,
            content.Name,
            isMedia ? "media" : "content",
            content.ContentType.Alias,
            content.Url(),
            thumbnailUrl,
            score,
            content.UpdateDate,
            GetContentPath(content),
            new Dictionary<string, object>
            {
                { "Level", content.Level },
                { "ContentTypeAlias", content.ContentType.Alias }
            });
    }
}

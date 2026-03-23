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
}

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Result of the search Umbraco tool.
/// </summary>
/// <param name="Success">Whether the search was successful.</param>
/// <param name="Results">The list of search results.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record SearchUmbracoResult(
    bool Success,
    IReadOnlyList<UmbracoSearchResultItem> Results,
    string? Message);

/// <summary>
/// A single search result item from Umbraco.
/// </summary>
/// <param name="Id">The unique identifier of the item.</param>
/// <param name="Name">The name of the item.</param>
/// <param name="Type">The type of item: "content" or "media".</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL of the item (if available).</param>
/// <param name="ThumbnailUrl">The thumbnail URL for media items (if available).</param>
/// <param name="Score">The search relevance score.</param>
/// <param name="UpdateDate">The last update date of the item.</param>
/// <param name="Path">The breadcrumb path to the item (e.g., "Home > News > Article").</param>
/// <param name="Metadata">Additional metadata about the item.</param>
public record UmbracoSearchResultItem(
    Guid Id,
    string Name,
    string Type,
    string ContentType,
    string? Url,
    string? ThumbnailUrl,
    double Score,
    DateTime UpdateDate,
    string Path,
    Dictionary<string, object> Metadata);

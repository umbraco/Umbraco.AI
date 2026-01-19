using System.ComponentModel;
using Examine;
using Examine.Search;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the SearchUmbraco tool.
/// </summary>
/// <param name="Query">The search query.</param>
/// <param name="Type">Filter by type: 'content', 'media', or 'all'.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
public record SearchUmbracoArgs(
    [property: Description("Search query to find content and media")]
    string Query,

    [property: Description("Filter by type: 'content', 'media', or 'all' (default)")]
    string? Type = "all",

    [property: Description("Maximum number of results to return (default 10, max 50)")]
    int? MaxResults = 10);

/// <summary>
/// Tool that searches Umbraco content and media using Examine.
/// </summary>
[AiTool("search_umbraco", "Search Umbraco", Category = "Umbraco")]
public class SearchUmbracoTool : AiToolBase<SearchUmbracoArgs>
{
    private const string ExternalIndexName = "ExternalIndex";

    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="SearchUmbracoTool"/>.
    /// </summary>
    /// <param name="examineManager">The Examine manager.</param>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public SearchUmbracoTool(
        IExamineManager examineManager,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _examineManager = examineManager;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Searches Umbraco content and media by text query. " +
        "Returns matching items with metadata including name, type, URL, and thumbnail for media. " +
        "Use type parameter to filter results: 'content' for content only, 'media' for media only, or 'all' for both."  +
        "Use the ID or Key from results to reference content or media items in other tools.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(SearchUmbracoArgs args, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(args.Query))
        {
            return Task.FromResult<object>(new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "Search query cannot be empty."));
        }

        // Sanitize type filter
        var typeFilter = args.Type?.ToLowerInvariant() ?? "all";
        if (typeFilter != "content" && typeFilter != "media" && typeFilter != "all")
        {
            return Task.FromResult<object>(new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                $"Invalid type filter '{args.Type}'. Must be 'content', 'media', or 'all'."));
        }

        // Enforce max results limit
        var maxResults = Math.Min(args.MaxResults ?? 10, 50);

        // Get Examine index
        if (!_examineManager.TryGetIndex(ExternalIndexName, out var index))
        {
            return Task.FromResult<object>(new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "Search index is not available."));
        }

        try
        {
            // Execute search
            var searchResults = PerformSearch(index, args.Query, typeFilter, maxResults);

            // Enrich results with published content data
            var enrichedResults = EnrichResults(searchResults);

            return Task.FromResult<object>(new SearchUmbracoResult(
                true,
                enrichedResults,
                null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<object>(new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                $"Search failed: {ex.Message}"));
        }
    }

    private ISearchResults PerformSearch(IIndex index, string query, string typeFilter, int maxResults)
    {
        var searcher = index.Searcher;
        var queryExecutor = searcher.CreateQuery();

        // Build query with type filter if needed
        IBooleanOperation? booleanQuery = null;

        if (typeFilter == "content")
        {
            booleanQuery = queryExecutor.Field("__IndexType", "content");
        }
        else if (typeFilter == "media")
        {
            booleanQuery = queryExecutor.Field("__IndexType", "media");
        }

        // Add the search term
        if (booleanQuery != null)
        {
            booleanQuery = booleanQuery.And().ManagedQuery(query);
        }
        else
        {
            booleanQuery = queryExecutor.ManagedQuery(query);
        }

        // Execute with options
        var results = booleanQuery.Execute(new QueryOptions(0, maxResults));

        return results;
    }

    private IReadOnlyList<UmbracoSearchResultItem> EnrichResults(ISearchResults searchResults)
    {
        var enrichedResults = new List<UmbracoSearchResultItem>();

        // Try to get Umbraco context for enrichment
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            // Return basic results without enrichment
            return searchResults.Select(r => CreateBasicResultItem(r)).ToList();
        }

        foreach (var searchResult in searchResults)
        {
            // Try to parse the ID
            if (!int.TryParse(searchResult.Id, out var nodeId))
            {
                continue;
            }

            // Determine type from index
            var isMedia = searchResult.Values.ContainsKey("__IndexType") &&
                         searchResult.Values["__IndexType"] == "media";

            IPublishedContent? publishedItem = null;

            if (isMedia)
            {
                publishedItem = umbracoContext.Media?.GetById(nodeId);
            }
            else
            {
                publishedItem = umbracoContext.Content?.GetById(nodeId);
            }

            if (publishedItem != null)
            {
                enrichedResults.Add(CreateEnrichedResultItem(publishedItem, searchResult.Score, isMedia));
            }
            else
            {
                // Fallback to basic result if not found in published cache
                enrichedResults.Add(CreateBasicResultItem(searchResult));
            }
        }

        return enrichedResults;
    }

    private UmbracoSearchResultItem CreateBasicResultItem(ISearchResult searchResult)
    {
        // Extract basic data from search result
        var name = searchResult.Values.ContainsKey("nodeName")
            ? searchResult.Values["nodeName"]
            : "Unknown";

        var contentType = searchResult.Values.ContainsKey("contentType")
            ? searchResult.Values["contentType"]
            : "Unknown";

        var updateDate = searchResult.Values.ContainsKey("updateDate")
            ? DateTime.TryParse(searchResult.Values["updateDate"], out var date) ? date : DateTime.MinValue
            : DateTime.MinValue;

        var isMedia = searchResult.Values.ContainsKey("__IndexType") &&
                     searchResult.Values["__IndexType"] == "media";

        return new UmbracoSearchResultItem(
            Guid.TryParse(searchResult.Id, out var guid) ? guid : Guid.Empty,
            name,
            isMedia ? "media" : "content",
            contentType,
            null, // No URL without published content
            null, // No thumbnail without published content
            searchResult.Score,
            updateDate,
            "Unknown",
            new Dictionary<string, object>());
    }

    private UmbracoSearchResultItem CreateEnrichedResultItem(IPublishedContent content, float score, bool isMedia)
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

    private string? GetMediaThumbnailUrl(IPublishedContent media)
    {
        // For images, generate a thumbnail URL with crop/resize
        if (media.ContentType.Alias.Contains("Image", StringComparison.OrdinalIgnoreCase))
        {
            var url = media.Url();
            if (!string.IsNullOrEmpty(url))
            {
                return $"{url}?width=200&height=200&mode=crop";
            }
        }

        // For other media types, return the URL as-is (or null)
        return media.Url();
    }

    private string GetContentPath(IPublishedContent content)
    {
        var pathParts = new List<string>();
        var current = content;

        while (current != null)
        {
            pathParts.Insert(0, current.Name);
            current = current.Parent();
        }

        return string.Join(" > ", pathParts);
    }
}

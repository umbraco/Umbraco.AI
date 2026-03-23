using System.ComponentModel;
using Examine;
using Examine.Search;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the SearchUmbraco tool.
/// </summary>
/// <param name="Query">The search query.</param>
/// <param name="Type">Filter by type: 'content', 'media', or 'all'.</param>
/// <param name="Tags">Filter by tags (exact match). Results must have at least one of the specified tags.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
public record SearchUmbracoArgs(
    [property: Description("Search query to find content and media. Use specific keywords or phrases for best results. Title matches are prioritized.")]
    string Query,

    [property: Description("Filter by type: 'content', 'media', or 'all' (default)")]
    string? Type = "all",

    [property: Description("Filter by tags (exact match). Results must have at least one of the specified tags.")]
    string[]? Tags = null,

    [property: Description("Maximum number of results to return (default 10, max 50)")]
    int? MaxResults = 10);

/// <summary>
/// Tool that searches Umbraco content and media using Examine.
/// </summary>
[AITool("search_umbraco", "Search Umbraco", ScopeId = SearchScope.ScopeId)]
public class SearchUmbracoTool : AIToolBase<SearchUmbracoArgs>
{
    private const string ExternalIndexName = "ExternalIndex";

    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="SearchUmbracoTool"/>.
    /// </summary>
    /// <param name="examineManager">The Examine manager.</param>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user context.</param>
    public SearchUmbracoTool(
        IExamineManager examineManager,
        IUmbracoContextAccessor umbracoContextAccessor,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _examineManager = examineManager;
        _umbracoContextAccessor = umbracoContextAccessor;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Searches Umbraco content and media by text query. " +
        "Searches across content fields and tags. " +
        "Returns matching items with metadata including name, type, URL, and thumbnail for media. " +
        "Use type parameter to filter results: 'content' for content only, 'media' for media only, or 'all' for both. " +
        "Use tags parameter to filter by exact tag values (results must have at least one matching tag). " +
        "**IMPORTANT** Use the ID or Key from results to reference content or media items in other tools.";

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
            // Resolve user start node restrictions for content/media access filtering
            var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            int[]? startContentIds = null;
            int[]? startMediaIds = null;

            if (user is not null)
            {
                startContentIds = GetEffectiveStartNodeIds(user.StartContentIds, user.Groups.Select(g => g.StartContentId));
                startMediaIds = GetEffectiveStartNodeIds(user.StartMediaIds, user.Groups.Select(g => g.StartMediaId));
            }

            // Execute search with start node filtering applied at query level
            var searchResults = PerformSearch(index, args.Query, typeFilter, args.Tags, maxResults, startContentIds, startMediaIds);

            // Enrich results with published content data
            var enrichedResults = EnrichResults(searchResults, startContentIds, startMediaIds);

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

    private ISearchResults PerformSearch(IIndex index, string query, string typeFilter, string[]? tags, int maxResults, int[]? startContentIds, int[]? startMediaIds)
    {
        var searcher = index.Searcher;
        var queryExecutor = searcher.CreateQuery();

        IBooleanOperation? booleanQuery = null;

        // Type filter
        if (typeFilter == "content")
        {
            booleanQuery = queryExecutor.Field("__IndexType", "content");
        }
        else if (typeFilter == "media")
        {
            booleanQuery = queryExecutor.Field("__IndexType", "media");
        }

        // Build boosted text query using native Lucene syntax
        var luceneQuery = BuildTextQuery(query);

        if (booleanQuery != null)
        {
            booleanQuery = booleanQuery.And().NativeQuery(luceneQuery);
        }
        else
        {
            booleanQuery = queryExecutor.NativeQuery(luceneQuery);
        }

        // Tag filter - only when explicit Tags parameter provided (pure filter, not mixed with text search)
        if (tags is { Length: > 0 })
        {
            booleanQuery = booleanQuery.And().GroupedOr(["tags"], tags);
        }

        // Start node access filter — restricts results to content/media the user can access.
        // The __Path field in Examine contains the ancestor chain (e.g., "-1,1234,5678"),
        // so filtering on it ensures only items under the user's allowed start nodes are returned.
        var pathFilter = BuildStartNodePathFilter(typeFilter, startContentIds, startMediaIds);
        if (pathFilter is not null)
        {
            booleanQuery = booleanQuery.And().NativeQuery(pathFilter);
        }

        return booleanQuery.Execute(new QueryOptions(0, maxResults));
    }

    internal static string BuildTextQuery(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Cap at 10 terms to prevent excessively complex queries
        if (terms.Length > 10)
        {
            terms = terms[..10];
        }

        var escapedTerms = terms.Select(EscapeLuceneTerm).ToArray();
        var isMultiWord = escapedTerms.Length > 1;

        var parts = new List<string>();

        // Boosted name field matches (individual terms)
        foreach (var term in escapedTerms)
        {
            parts.Add($"nodeName:{term}^10");
        }

        // Exact phrase match on name (multi-word only)
        if (isMultiWord)
        {
            var phrase = string.Join(" ", escapedTerms);
            parts.Add($"nodeName:\"{phrase}\"^15");
        }

        // Broad field search (individual terms, baseline boost)
        foreach (var term in escapedTerms)
        {
            parts.Add($"{term}^1");
        }

        // Phrase match across default fields (multi-word only)
        if (isMultiWord)
        {
            var phrase = string.Join(" ", escapedTerms);
            parts.Add($"\"{phrase}\"^3");
        }

        return string.Join(" ", parts);
    }

    internal static string EscapeLuceneTerm(string term)
    {
        var sb = new System.Text.StringBuilder(term.Length);
        foreach (var c in term)
        {
            if (c is '+' or '-' or '!' or '(' or ')' or '{' or '}' or '[' or ']' or '^' or '"' or '~' or '*' or '?' or ':' or '\\' or '/')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes effective start node IDs by combining user-level and group-level start nodes.
    /// User-level start nodes take precedence; if not set, group start nodes are used.
    /// </summary>
    internal static int[]? GetEffectiveStartNodeIds(int[]? userStartNodeIds, IEnumerable<int?> groupStartNodeIds)
    {
        // User-level start nodes take precedence when set
        if (userStartNodeIds is { Length: > 0 })
        {
            return userStartNodeIds;
        }

        // Fall back to group start nodes
        var groupIds = groupStartNodeIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return groupIds.Length > 0 ? groupIds : null;
    }

    /// <summary>
    /// Builds a Lucene query fragment to filter results by the user's start node path restrictions.
    /// Returns null when no filtering is needed.
    /// </summary>
    internal static string? BuildStartNodePathFilter(string typeFilter, int[]? startContentIds, int[]? startMediaIds)
    {
        var contentRestricted = !IsUnrestricted(startContentIds);
        var mediaRestricted = !IsUnrestricted(startMediaIds);

        // When searching a specific type, only that type's restrictions matter
        if (typeFilter == "content")
        {
            return contentRestricted ? BuildPathOrClause(startContentIds!) : null;
        }

        if (typeFilter == "media")
        {
            return mediaRestricted ? BuildPathOrClause(startMediaIds!) : null;
        }

        // typeFilter == "all": both content and media
        if (!contentRestricted && !mediaRestricted)
        {
            return null;
        }

        if (contentRestricted && mediaRestricted)
        {
            // Both restricted — combine all start node IDs (content/media have separate path trees)
            var allIds = startContentIds!.Concat(startMediaIds!).Distinct().ToArray();
            return BuildPathOrClause(allIds);
        }

        // Mixed: one restricted, one unrestricted.
        // Allow all items of the unrestricted type, restrict the other by path.
        if (contentRestricted)
        {
            // Media is unrestricted; content must match start nodes
            var pathClause = BuildPathOrClause(startContentIds!);
            return $"(__IndexType:media OR ({pathClause}))";
        }

        // Content is unrestricted; media must match start nodes
        var mediaPathClause = BuildPathOrClause(startMediaIds!);
        return $"(__IndexType:content OR ({mediaPathClause}))";
    }

    internal static bool IsUnrestricted(int[]? ids)
        => ids is null or { Length: 0 } || Array.Exists(ids, static id => id == -1);

    private static string BuildPathOrClause(int[] startNodeIds)
    {
        // __Path is indexed as a tokenized field with comma-separated IDs.
        // Each start node ID is searched as a term within the path.
        var clauses = startNodeIds.Select(id => $"__Path:{id}");
        return $"({string.Join(" OR ", clauses)})";
    }

    private IReadOnlyList<UmbracoSearchResultItem> EnrichResults(ISearchResults searchResults, int[]? startContentIds, int[]? startMediaIds)
    {
        var hasRestrictions = !IsUnrestricted(startContentIds) || !IsUnrestricted(startMediaIds);
        var enrichedResults = new List<UmbracoSearchResultItem>();

        // Try to get Umbraco context for enrichment
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            // Without Umbraco context we can't verify paths for access control.
            // If the user has start node restrictions, skip basic results to avoid leaking content.
            if (hasRestrictions)
            {
                return enrichedResults;
            }

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
            else if (!hasRestrictions)
            {
                // Fallback to basic result only when there are no start node restrictions,
                // since we can't verify the path without IPublishedContent.
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

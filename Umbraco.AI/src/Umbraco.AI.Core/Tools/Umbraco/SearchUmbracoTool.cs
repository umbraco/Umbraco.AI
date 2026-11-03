using System.ComponentModel;
using Examine;
using Examine.Search;

using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.DependencyInjection;
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

    // How many index matches to fetch per result asked for, and the most to fetch, before access filtering.
    private const int CandidateMultiplier = 5;
    private const int MaxCandidates = 250;

    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IUmbracoReadAuthorizer _readAuthorizer;

    /// <summary>
    /// Initializes a new instance of <see cref="SearchUmbracoTool"/>.
    /// </summary>
    /// <param name="examineManager">The Examine manager.</param>
    /// <param name="umbracoContextFactory">The Umbraco context factory.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user context.</param>
    [Obsolete("Use the constructor that accepts an IUmbracoReadAuthorizer. Will be removed in v20.")]
    public SearchUmbracoTool(
        IExamineManager examineManager,
        IUmbracoContextFactory umbracoContextFactory,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        : this(
            examineManager,
            umbracoContextFactory,
            backOfficeSecurityAccessor,
            StaticServiceProvider.Instance.GetRequiredService<IUmbracoReadAuthorizer>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchUmbracoTool"/>.
    /// </summary>
    /// <param name="examineManager">The Examine manager.</param>
    /// <param name="umbracoContextFactory">The Umbraco context factory.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user context.</param>
    /// <param name="readAuthorizer">Limits results to what the acting user can access.</param>
    public SearchUmbracoTool(
        IExamineManager examineManager,
        IUmbracoContextFactory umbracoContextFactory,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IUmbracoReadAuthorizer readAuthorizer)
    {
        _examineManager = examineManager;
        _umbracoContextFactory = umbracoContextFactory;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _readAuthorizer = readAuthorizer;
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
    protected override async Task<object> ExecuteAsync(SearchUmbracoArgs args, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(args.Query))
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "Search query cannot be empty.");
        }

        // Sanitize type filter
        var typeFilter = args.Type?.ToLowerInvariant() ?? "all";
        if (typeFilter != "content" && typeFilter != "media" && typeFilter != "all")
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                $"Invalid type filter '{args.Type}'. Must be 'content', 'media', or 'all'.");
        }

        // Enforce max results limit
        var maxResults = Math.Min(args.MaxResults ?? 10, 50);

        // Get Examine index
        if (!_examineManager.TryGetIndex(ExternalIndexName, out var index))
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "Search index is not available.");
        }

        try
        {
            // Search results show names, keys and places in the tree, so they need an acting user who can
            // browse the tree; results are then limited to that user's start nodes.
            var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            if (user is null || !UmbracoSectionAccess.CanBrowseTree(user))
            {
                return new SearchUmbracoResult(
                    false,
                    Array.Empty<UmbracoSearchResultItem>(),
                    "You do not have access to search content or media.");
            }

            // Access is decided per result by the read authorizer (start nodes, sections, permissions, as
            // the CMS tree does). Fetch extra matches so a user restricted to part of the tree still gets
            // up to maxResults of what they can see.
            var searchResults = PerformSearch(index, args.Query, typeFilter, args.Tags, Math.Min(maxResults * CandidateMultiplier, MaxCandidates));

            // Enrich results with published content data, keeping only what the user may see
            var enrichedResults = (await EnrichResultsAsync(searchResults)).Take(maxResults).ToList();

            return new SearchUmbracoResult(
                true,
                enrichedResults,
                null);
        }
        catch (Exception ex)
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                $"Search failed: {ex.Message}");
        }
    }

    private ISearchResults PerformSearch(IIndex index, string query, string typeFilter, string[]? tags, int maxResults)
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

    private async Task<IReadOnlyList<UmbracoSearchResultItem>> EnrichResultsAsync(ISearchResults searchResults)
    {
        var enrichedResults = new List<UmbracoSearchResultItem>();

        // Ensure UmbracoContext exists — not automatically created for backoffice API requests, and
        // never created at all when this tool is invoked from Umbraco.Automate's background dispatcher.
        // A no-op if a context is already ambient. Every result is checked against the published item,
        // so a result that can't be loaded is dropped rather than returned unverified.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var umbracoContext = contextReference.UmbracoContext;

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

            IPublishedContent? publishedItem = isMedia
                ? umbracoContext.Media?.GetById(nodeId)
                : umbracoContext.Content?.GetById(nodeId);

            if (publishedItem is null)
            {
                continue;
            }

            var canSee = isMedia
                ? await _readAuthorizer.CanReadMediaAsync(publishedItem.Key)
                : await _readAuthorizer.CanSeeContentInTreeAsync(publishedItem.Key, publishedItem.Path);

            if (canSee)
            {
                enrichedResults.Add(CreateEnrichedResultItem(publishedItem, searchResult.Score, isMedia));
            }
        }

        return enrichedResults;
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
            ContentToolHelpers.GetContentPath(content),
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

}

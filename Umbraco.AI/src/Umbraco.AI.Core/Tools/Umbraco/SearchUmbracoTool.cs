using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Search;
using Umbraco.Cms.Core.Search.Querying;
using Umbraco.Cms.Core.Search.Querying.Filtering;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
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
/// Tool that searches Umbraco content and media using the CMS search indexes.
/// </summary>
[AITool("search_umbraco", "Search Umbraco", ScopeId = SearchScope.ScopeId)]
public class SearchUmbracoTool : AIToolBase<SearchUmbracoArgs>
{
    // How many index matches to fetch per result asked for, and the most to fetch, before access filtering.
    private const int CandidateMultiplier = 5;
    private const int MaxCandidates = 250;

    // The CMS searcher requires every word of a query to match. When that finds too little, each word is
    // searched on its own; this caps how many words are tried so the number of searcher calls stays bounded.
    private const int MaxFallbackWords = 5;

    private readonly ISearcherResolver _searcherResolver;
    private readonly ILanguageService _languageService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IUmbracoReadAuthorizer _readAuthorizer;

    /// <summary>
    /// Initializes a new instance of <see cref="SearchUmbracoTool"/>.
    /// </summary>
    /// <param name="searcherResolver">Resolves the searcher for a search index.</param>
    /// <param name="languageService">The language service, used to search every culture.</param>
    /// <param name="umbracoContextFactory">The Umbraco context factory.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user context.</param>
    /// <param name="readAuthorizer">Limits results to what the acting user can access.</param>
    public SearchUmbracoTool(
        ISearcherResolver searcherResolver,
        ILanguageService languageService,
        IUmbracoContextFactory umbracoContextFactory,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IUmbracoReadAuthorizer readAuthorizer)
    {
        _searcherResolver = searcherResolver;
        _languageService = languageService;
        _umbracoContextFactory = umbracoContextFactory;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _readAuthorizer = readAuthorizer;
    }

    /// <inheritdoc />
    public override string Description =>
        "Searches Umbraco content and media by text query. " +
        "Searches across content fields and tags. " +
        "Short keyword queries work best: every word of the query must match, and if that finds too little, " +
        "the words are searched individually. Avoid long sentences. " +
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
            var plan = await CreateSearchPlanAsync(typeFilter, args.Tags);
            if (plan is null)
            {
                return new SearchUmbracoResult(
                    false,
                    Array.Empty<UmbracoSearchResultItem>(),
                    "Search index is not available.");
            }

            var take = Math.Min(maxResults * CandidateMultiplier, MaxCandidates);
            var enrichedResults = await SearchAndEnrichAsync(plan, args.Query, take, maxResults);

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

    /// <summary>
    /// Works out which indexes, searchers, cultures and filters a search covers. Returns null when a searched
    /// index has no searcher.
    /// </summary>
    private async Task<SearchPlan?> CreateSearchPlanAsync(string typeFilter, string[]? tags)
    {
        var aliases = new List<(string Alias, bool IsMedia)>();
        if (typeFilter != "media")
        {
            aliases.Add((global::Umbraco.Cms.Core.Constants.Search.IndexAliases.PublishedContent, false));
        }

        if (typeFilter != "content")
        {
            aliases.Add((global::Umbraco.Cms.Core.Constants.Search.IndexAliases.DraftMedia, true));
        }

        var indexes = new List<SearchIndex>();
        foreach (var (alias, isMedia) in aliases)
        {
            var searcher = _searcherResolver.GetSearcher(alias);
            if (searcher is null)
            {
                return null;
            }

            indexes.Add(new SearchIndex(alias, isMedia, searcher));
        }

        // Tag filter - only when explicit Tags parameter provided. Values within a filter are ORed.
        Filter[]? filters = tags is { Length: > 0 }
            ? [new KeywordFilter(global::Umbraco.Cms.Core.Constants.Search.FieldNames.Tags, tags, false)]
            : null;

        var cultures = (await _languageService.GetAllAsync()).Select(x => x.IsoCode).ToArray();
        if (cultures.Length == 0)
        {
            cultures = [string.Empty];
        }

        return new SearchPlan(indexes, cultures, filters);
    }

    /// <summary>
    /// Runs the search in up to two passes and returns the items the user may see, best match first. The first
    /// pass sends the query as typed (the CMS searcher requires every word to match). If that fills fewer than
    /// <paramref name="maxResults"/>, and the query has several words, a second pass searches each word on its
    /// own and appends what is new. The searcher is called at most indexes x cultures x (1 + MaxFallbackWords)
    /// times; the published cache and the authorizer are consulted for at most MaxCandidates distinct items.
    /// </summary>
    private async Task<IReadOnlyList<UmbracoSearchResultItem>> SearchAndEnrichAsync(
        SearchPlan plan,
        string query,
        int take,
        int maxResults)
    {
        // Ensure UmbracoContext exists — not automatically created for backoffice API requests, and
        // never created at all when this tool is invoked from Umbraco.Automate's background dispatcher.
        // A no-op if a context is already ambient. Every result is checked against the published item,
        // so a result that can't be loaded is dropped rather than returned unverified.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var umbracoContext = contextReference.UmbracoContext;

        var results = new List<UmbracoSearchResultItem>();
        var examined = new HashSet<Guid>();

        var candidates = MergeByRank(await SearchAsync(plan, [query], take), examined, MaxCandidates);
        await EnrichResultsAsync(umbracoContext, candidates, examined, results, maxResults);

        var words = query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var candidateBudget = MaxCandidates - examined.Count;
        if (results.Count < maxResults && words.Length > 1 && candidateBudget > 0)
        {
            var fallback = MergeByRank(await SearchAsync(plan, words.Take(MaxFallbackWords), take), examined, candidateBudget);
            await EnrichResultsAsync(umbracoContext, fallback, examined, results, maxResults);
        }

        // The search abstractions return documents in rank order without a score, so the score is
        // synthesized from the final order (highest first).
        return results.Select((item, index) => item with { Score = results.Count - index }).ToList();
    }

    /// <summary>
    /// Searches every index and culture for each query (invariant content is part of every culture's results).
    /// Each returned list is one search's hits in rank order.
    /// </summary>
    private static async Task<IReadOnlyList<IReadOnlyList<SearchHit>>> SearchAsync(
        SearchPlan plan,
        IEnumerable<string> queries,
        int take)
    {
        var rankedLists = new List<IReadOnlyList<SearchHit>>();
        foreach (var query in queries)
        {
            foreach (var index in plan.Indexes)
            {
                foreach (var culture in plan.Cultures)
                {
                    // A null access context only matches unprotected content, the same as the CMS's public
                    // search; member-protected pages must not be offered to the model.
                    var result = await index.Searcher.SearchAsync(
                        index.Alias,
                        query: query,
                        filters: plan.Filters,
                        culture: culture.Length == 0 ? null : culture,
                        accessContext: null,
                        skip: 0,
                        take: take);

                    rankedLists.Add(result.Documents.Select(d => new SearchHit(d.Id, index.IsMedia)).ToList());
                }
            }
        }

        return rankedLists;
    }

    /// <summary>
    /// Interleaves ranked lists by rank, dropping repeats (the same item can match in several cultures,
    /// indexes or words) and anything already examined, and stops at <paramref name="limit"/> hits. Lists
    /// are given in priority order, so on a tie the earlier list wins.
    /// </summary>
    private static IReadOnlyList<SearchHit> MergeByRank(
        IReadOnlyList<IReadOnlyList<SearchHit>> rankedLists,
        HashSet<Guid> examined,
        int limit)
    {
        var merged = new List<SearchHit>();
        var seen = new HashSet<Guid>();
        var longest = rankedLists.Count == 0 ? 0 : rankedLists.Max(x => x.Count);
        for (var rank = 0; rank < longest && merged.Count < limit; rank++)
        {
            foreach (var list in rankedLists)
            {
                if (merged.Count >= limit)
                {
                    break;
                }

                if (rank < list.Count && !examined.Contains(list[rank].Key) && seen.Add(list[rank].Key))
                {
                    merged.Add(list[rank]);
                }
            }
        }

        return merged;
    }

    /// <summary>
    /// Adds the candidates the user may see to <paramref name="results"/>, in order, stopping once
    /// <paramref name="maxResults"/> are held so later candidates cost no lookups.
    /// </summary>
    private async Task EnrichResultsAsync(
        IUmbracoContext umbracoContext,
        IReadOnlyList<SearchHit> candidates,
        HashSet<Guid> examined,
        List<UmbracoSearchResultItem> results,
        int maxResults)
    {
        foreach (var (key, isMedia) in candidates)
        {
            if (results.Count >= maxResults)
            {
                return;
            }

            examined.Add(key);

            IPublishedContent? publishedItem = isMedia
                ? umbracoContext.Media?.GetById(key)
                : umbracoContext.Content?.GetById(key);

            if (publishedItem is null)
            {
                continue;
            }

            var canSee = isMedia
                ? await _readAuthorizer.CanReadMediaAsync(publishedItem.Key)
                : await _readAuthorizer.CanSeeContentInTreeAsync(publishedItem.Key, publishedItem.Path);

            if (canSee)
            {
                results.Add(CreateEnrichedResultItem(publishedItem, 0, isMedia));
            }
        }
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

    private readonly record struct SearchHit(Guid Key, bool IsMedia);

    private sealed record SearchIndex(string Alias, bool IsMedia, ISearcher Searcher);

    private sealed record SearchPlan(IReadOnlyList<SearchIndex> Indexes, string[] Cultures, Filter[]? Filters);
}

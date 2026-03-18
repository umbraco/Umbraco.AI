using System.ComponentModel;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the SemanticSearchUmbraco tool.
/// </summary>
/// <param name="Query">The natural language search query.</param>
/// <param name="Type">Filter by type: 'content', 'media', or 'all'.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
public record SemanticSearchUmbracoArgs(
    [property: Description("Natural language search query to find semantically similar content and media")]
    string Query,

    [property: Description("Filter by type: 'content', 'media', or 'all' (default)")]
    string? Type = "all",

    [property: Description("Maximum number of results to return (default 10, max 50)")]
    int? MaxResults = 10);

/// <summary>
/// Tool that searches Umbraco content and media using semantic/vector similarity.
/// Uses embedding vectors and cosine similarity for natural language understanding.
/// </summary>
[AITool("semantic_search_umbraco", "Semantic Search Umbraco", ScopeId = SearchScope.ScopeId)]
public class SemanticSearchUmbracoTool : AIToolBase<SemanticSearchUmbracoArgs>
{
    private readonly IAISemanticSearchService _service;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly AISemanticSearchOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SemanticSearchUmbracoTool"/>.
    /// </summary>
    internal SemanticSearchUmbracoTool(
        IAISemanticSearchService service,
        IUmbracoContextAccessor umbracoContextAccessor,
        IOptions<AISemanticSearchOptions> options)
    {
        _service = service;
        _umbracoContextAccessor = umbracoContextAccessor;
        _options = options.Value;
    }

    /// <inheritdoc />
    public override string Description =>
        "Searches Umbraco content and media using semantic/natural language understanding. " +
        "Use this when searching by meaning, concept, or question rather than exact keywords. " +
        "Returns items semantically similar to the query, ranked by relevance. " +
        "Prefer this over keyword search when the query is a question or concept. " +
        "Use type parameter to filter: 'content', 'media', or 'all'. " +
        "**IMPORTANT** Use the ID or Key from results to reference content or media items in other tools.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(SemanticSearchUmbracoArgs args, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "Semantic search is not enabled. Configure 'Umbraco:AI:SemanticSearch:Enabled' to true.");
        }

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

        var maxResults = Math.Min(args.MaxResults ?? 10, 50);

        try
        {
            var queryOptions = new SemanticSearchQueryOptions(
                TypeFilter: typeFilter == "all" ? null : typeFilter,
                MaxResults: maxResults);

            var results = await _service.SearchAsync(args.Query, queryOptions, cancellationToken);

            var enrichedResults = EnrichResults(results);

            return new SearchUmbracoResult(true, enrichedResults, null);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("profile", StringComparison.OrdinalIgnoreCase))
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                "No embedding profile configured. Set up a default embedding profile in Umbraco AI settings.");
        }
        catch (Exception ex)
        {
            return new SearchUmbracoResult(
                false,
                Array.Empty<UmbracoSearchResultItem>(),
                $"Semantic search failed: {ex.Message}");
        }
    }

    private IReadOnlyList<UmbracoSearchResultItem> EnrichResults(IReadOnlyList<SemanticSearchResult> results)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            // Return basic results without enrichment
            return results.Select(r => new UmbracoSearchResultItem(
                r.ContentKey,
                r.Name,
                r.ContentType,
                r.ContentTypeAlias,
                null,
                null,
                r.SimilarityScore,
                DateTime.MinValue,
                "Unknown",
                new Dictionary<string, object>())).ToList();
        }

        var enrichedResults = new List<UmbracoSearchResultItem>();

        foreach (var result in results)
        {
            var isMedia = string.Equals(result.ContentType, "media", StringComparison.OrdinalIgnoreCase);

            IPublishedContent? publishedItem = isMedia
                ? umbracoContext.Media?.GetById(result.ContentKey)
                : umbracoContext.Content?.GetById(result.ContentKey);

            if (publishedItem is not null)
            {
                enrichedResults.Add(CreateEnrichedResultItem(publishedItem, result.SimilarityScore, isMedia));
            }
            else
            {
                // Fallback to basic result
                enrichedResults.Add(new UmbracoSearchResultItem(
                    result.ContentKey,
                    result.Name,
                    result.ContentType,
                    result.ContentTypeAlias,
                    null,
                    null,
                    result.SimilarityScore,
                    DateTime.MinValue,
                    "Unknown",
                    new Dictionary<string, object>()));
            }
        }

        return enrichedResults;
    }

    private static UmbracoSearchResultItem CreateEnrichedResultItem(IPublishedContent content, float score, bool isMedia)
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

    private static string? GetMediaThumbnailUrl(IPublishedContent media)
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

    private static string GetContentPath(IPublishedContent content)
    {
        var pathParts = new List<string>();
        var current = content;

        while (current is not null)
        {
            pathParts.Insert(0, current.Name);
            current = current.Parent();
        }

        return string.Join(" > ", pathParts);
    }
}

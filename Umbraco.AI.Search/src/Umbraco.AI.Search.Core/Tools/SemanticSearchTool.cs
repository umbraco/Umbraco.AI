using System.ComponentModel;
using System.Numerics.Tensors;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Search.Core.Tools;

/// <summary>
/// Arguments for the semantic search tool.
/// </summary>
/// <param name="Query">Text to search for semantically similar content. Required if DocumentId is not provided.</param>
/// <param name="DocumentId">Find content similar to this document (by GUID). Uses the document's stored embeddings directly — no re-embedding needed. Required if Query is not provided.</param>
/// <param name="Culture">Optional culture filter for multilingual content.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
public record SemanticSearchArgs(
    [property: Description("Text to find semantically similar content for. Can be a phrase, sentence, or paragraph. Use this OR DocumentId, not both.")]
    string? Query = null,

    [property: Description("Find content similar to this document by its ID (GUID). Uses the document's existing embeddings — ideal for 'find similar pages' from the current content item. Use this OR Query, not both.")]
    string? DocumentId = null,

    [property: Description("Optional culture code to filter results (e.g., 'en-US', 'da-DK'). Leave empty for all cultures.")]
    string? Culture = null,

    [property: Description("Maximum number of results to return (default 10, max 50)")]
    int? MaxResults = 10);

/// <summary>
/// Tool that performs semantic similarity search over indexed Umbraco content using vector embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Supports two modes:
/// <list type="bullet">
///   <item><b>Text query</b> — delegates to <see cref="AIVectorSearcher"/> to embed and search.</item>
///   <item><b>Document ID</b> — retrieves stored embeddings for the document, averages them, and finds similar content (excluding the source document).</item>
/// </list>
/// </para>
/// </remarks>
[AITool("semantic_search", "Semantic Search", ScopeId = SearchScope.ScopeId)]
public class SemanticSearchTool : AIToolBase<SemanticSearchArgs>
{
    private static readonly string IndexName = AISearchConstants.IndexAliases.Search;

    private readonly AIVectorSearcher _searcher;
    private readonly IAIVectorStore _vectorStore;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ILogger<SemanticSearchTool> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SemanticSearchTool"/>.
    /// </summary>
    public SemanticSearchTool(
        AIVectorSearcher searcher,
        IAIVectorStore vectorStore,
        IUmbracoContextAccessor umbracoContextAccessor,
        ILogger<SemanticSearchTool> logger)
    {
        _searcher = searcher;
        _vectorStore = vectorStore;
        _umbracoContextAccessor = umbracoContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string Description =>
        "Searches Umbraco content using semantic similarity (meaning-based search). " +
        "Two modes: (1) provide a text Query to find content about that topic, or " +
        "(2) provide a DocumentId to find content similar to an existing page — perfect for discovering related content from the current content item in context. " +
        "Returns matching content items with name, type, URL, and similarity score. " +
        "**IMPORTANT** Use the Key from results to reference content items in other tools like get_umbraco_content.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(SemanticSearchArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(args.Query) && string.IsNullOrWhiteSpace(args.DocumentId))
        {
            return new SemanticSearchResult(false, [], "Either Query or DocumentId must be provided.");
        }

        var maxResults = Math.Min(args.MaxResults ?? 10, 50);

        try
        {
            List<AIVectorSearchResult> deduplicated;

            if (!string.IsNullOrWhiteSpace(args.DocumentId))
            {
                deduplicated = await SearchByDocumentAsync(args.DocumentId.Trim(), args.Culture, cancellationToken);
            }
            else
            {
                deduplicated = await _searcher.SearchByQueryAsync(IndexName, args.Query!, args.Culture, cancellationToken);
            }

            List<SemanticSearchResultItem> results = EnrichResults(
                deduplicated.Take(maxResults).ToList());

            return new SemanticSearchResult(true, results, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed for query: {Query}, documentId: {DocumentId}", args.Query, args.DocumentId);
            return new SemanticSearchResult(false, [], $"Semantic search failed: {ex.Message}");
        }
    }

    private async Task<List<AIVectorSearchResult>> SearchByDocumentAsync(
        string documentId, string? culture, CancellationToken cancellationToken)
    {
        IReadOnlyList<AIVectorEntry> storedVectors = await _vectorStore.GetVectorsByDocumentAsync(
            IndexName, documentId, culture, cancellationToken);

        if (storedVectors.Count == 0)
        {
            return [];
        }

        ReadOnlyMemory<float> queryVector = AverageVectors(storedVectors.Select(v => v.Vector).ToList());

        List<AIVectorSearchResult> results = await _searcher.SearchByVectorAsync(
            IndexName, queryVector, culture, cancellationToken);

        // Exclude the source document
        return results.Where(r => r.DocumentId != documentId).ToList();
    }

    private List<SemanticSearchResultItem> EnrichResults(List<AIVectorSearchResult> vectorResults)
    {
        _umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext);

        return vectorResults.Select(r =>
        {
            var objectType = r.Metadata?.TryGetValue("objectType", out var objType) == true
                ? objType.ToString()
                : null;

            // Try to resolve published content for richer results
            if (Guid.TryParse(r.DocumentId, out Guid key) && umbracoContext is not null)
            {
                IPublishedContent? content = objectType == nameof(UmbracoObjectTypes.Media)
                    ? umbracoContext.Media?.GetById(key)
                    : umbracoContext.Content?.GetById(key);

                if (content is not null)
                {
                    return new SemanticSearchResultItem(
                        r.DocumentId,
                        content.Name ?? r.DocumentId,
                        content.ContentType.Alias,
                        objectType == nameof(UmbracoObjectTypes.Media) ? "media" : "content",
                        content.Url(),
                        GetContentPath(content),
                        r.Score);
                }
            }

            // Fallback: return with just the document ID
            return new SemanticSearchResultItem(
                r.DocumentId,
                r.DocumentId,
                null,
                objectType,
                null,
                null,
                r.Score);
        }).ToList();
    }

    private static string GetContentPath(IPublishedContent content)
    {
        var ancestors = content.Ancestors();
        var pathParts = ancestors.Reverse().Select(a => a.Name).ToList();
        pathParts.Add(content.Name);
        return string.Join(" > ", pathParts);
    }

    /// <summary>
    /// Averages multiple vectors into a single representative vector.
    /// Used when a document has multiple chunks — the average captures the overall topic.
    /// </summary>
    private static ReadOnlyMemory<float> AverageVectors(IReadOnlyList<ReadOnlyMemory<float>> vectors)
    {
        if (vectors.Count == 1)
        {
            return vectors[0];
        }

        var dimensions = vectors[0].Length;
        var sum = new float[dimensions];

        foreach (ReadOnlyMemory<float> vector in vectors)
        {
            TensorPrimitives.Add(sum, vector.Span, sum);
        }

        TensorPrimitives.Divide(sum, vectors.Count, sum);
        return sum;
    }
}

/// <summary>
/// Result of the semantic search tool.
/// </summary>
/// <param name="Success">Whether the search was successful.</param>
/// <param name="Results">The list of search results ordered by similarity.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record SemanticSearchResult(
    bool Success,
    IReadOnlyList<SemanticSearchResultItem> Results,
    string? Message);

/// <summary>
/// A single semantic search result item.
/// </summary>
/// <param name="Key">The document key (GUID string).</param>
/// <param name="Name">The content name.</param>
/// <param name="ContentType">The content type alias (e.g., "blogPost", "article").</param>
/// <param name="Type">The item type: "content" or "media".</param>
/// <param name="Url">The public URL of the item (if available).</param>
/// <param name="Path">The breadcrumb path (e.g., "Home > Blog > Article").</param>
/// <param name="Score">The similarity score (0-1, higher is more similar).</param>
public record SemanticSearchResultItem(
    string Key,
    string Name,
    string? ContentType,
    string? Type,
    string? Url,
    string? Path,
    double Score);

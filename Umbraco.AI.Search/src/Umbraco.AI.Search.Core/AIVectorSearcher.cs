using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.AI.Search.Core;

/// <summary>
/// An <see cref="ISearcher"/> implementation that performs semantic similarity search
/// using vector embeddings.
/// </summary>
public sealed class AIVectorSearcher : ISearcher
{
    private readonly IAIVectorStore _vectorStore;
    private readonly IAIEmbeddingService _embeddingService;
    private readonly IOptions<AIVectorSearchOptions> _options;
    private readonly ILogger<AIVectorSearcher> _logger;

    public AIVectorSearcher(
        IAIVectorStore vectorStore,
        IAIEmbeddingService embeddingService,
        IOptions<AIVectorSearchOptions> options,
        ILogger<AIVectorSearcher> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        string? culture,
        string? segment,
        AccessContext? accessContext,
        int skip,
        int take,
        int maxSuggestions)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult(0, [], [], null);
        }

        try
        {
            List<AIVectorSearchResult> deduplicated = await SearchByQueryAsync(indexAlias, query, culture);

            // Apply pagination after deduplication
            List<AIVectorSearchResult> paged = deduplicated
                .Skip(skip)
                .Take(take)
                .ToList();

            IEnumerable<Document> documents = paged.Select(r =>
            {
                Guid.TryParse(r.DocumentId, out Guid id);
                var objectType = UmbracoObjectTypes.Document;

                if (r.Metadata?.TryGetValue("objectType", out var objTypeStr) == true
                    && Enum.TryParse(objTypeStr?.ToString(), out UmbracoObjectTypes parsed))
                {
                    objectType = parsed;
                }

                return new Document(id, objectType);
            });

            return new SearchResult(deduplicated.Count, documents, [], null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed for index {IndexAlias}", indexAlias);
            return new SearchResult(0, [], [], null);
        }
    }

    /// <summary>
    /// Performs a semantic search by text query and returns scored, deduplicated results.
    /// </summary>
    /// <param name="indexAlias">The index to search.</param>
    /// <param name="query">The text query to embed and search for.</param>
    /// <param name="culture">Optional culture filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deduplicated results ordered by similarity score (descending).</returns>
    internal async Task<List<AIVectorSearchResult>> SearchByQueryAsync(
        string indexAlias,
        string query,
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        Embedding<float> queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
        return await SearchByVectorAsync(indexAlias, queryEmbedding.Vector, culture, cancellationToken);
    }

    /// <summary>
    /// Performs a semantic search by vector and returns scored, deduplicated results.
    /// </summary>
    /// <param name="indexAlias">The index to search.</param>
    /// <param name="queryVector">The query vector to search with.</param>
    /// <param name="culture">Optional culture filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deduplicated results ordered by similarity score (descending).</returns>
    internal async Task<List<AIVectorSearchResult>> SearchByVectorAsync(
        string indexAlias,
        ReadOnlyMemory<float> queryVector,
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        var topK = _options.Value.DefaultTopK;
        IReadOnlyList<AIVectorSearchResult> vectorResults = await _vectorStore.SearchAsync(
            indexAlias,
            queryVector,
            culture,
            topK,
            cancellationToken);

        // Deduplicate by document: multiple chunks from the same document may match.
        // Use the best chunk score (max) per document.
        return vectorResults
            .GroupBy(r => r.DocumentId)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score)
            .ToList();
    }
}

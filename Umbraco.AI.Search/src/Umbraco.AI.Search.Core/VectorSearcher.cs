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
public sealed class VectorSearcher : ISearcher
{
    private readonly IVectorStore _vectorStore;
    private readonly IAIEmbeddingService _embeddingService;
    private readonly IOptions<VectorSearchOptions> _options;
    private readonly ILogger<VectorSearcher> _logger;

    public VectorSearcher(
        IVectorStore vectorStore,
        IAIEmbeddingService embeddingService,
        IOptions<VectorSearchOptions> options,
        ILogger<VectorSearcher> logger)
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
            Embedding<float> queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            var topK = _options.Value.DefaultTopK;
            IReadOnlyList<VectorSearchResult> vectorResults = await _vectorStore.SearchAsync(
                indexAlias,
                queryEmbedding.Vector,
                topK);

            // Apply pagination
            List<VectorSearchResult> paged = vectorResults
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

            return new SearchResult(vectorResults.Count, documents, [], null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed for index {IndexAlias}", indexAlias);
            return new SearchResult(0, [], [], null);
        }
    }
}

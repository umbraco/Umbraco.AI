using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.AI.Search.Core;

/// <summary>
/// An <see cref="IIndexer"/> implementation that generates vector embeddings from content
/// fields and stores them in an <see cref="IAIVectorStore"/>.
/// </summary>
/// <remarks>
/// Content is split into chunks before embedding to handle documents that exceed
/// embedding model token limits and to improve retrieval granularity.
/// </remarks>
public sealed class AIVectorIndexer : IIndexer
{
    private readonly IAIVectorStore _vectorStore;
    private readonly IAIEmbeddingService _embeddingService;
    private readonly IAITextChunker _textChunker;
    private readonly IOptions<AIVectorSearchOptions> _options;
    private readonly ILogger<AIVectorIndexer> _logger;

    public AIVectorIndexer(
        IAIVectorStore vectorStore,
        IAIEmbeddingService embeddingService,
        IAITextChunker textChunker,
        IOptions<AIVectorSearchOptions> options,
        ILogger<AIVectorIndexer> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _textChunker = textChunker;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(
        string indexAlias,
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection)
    {
        var documentId = id.ToString("D");

        // Group fields by culture to generate separate embeddings per language variant.
        // Fields without a culture are grouped under null (invariant content).
        var fieldsByCulture = fields
            .GroupBy(f => f.Culture)
            .ToList();

        if (fieldsByCulture.Count == 0)
        {
            _logger.LogDebug("No fields to index for document {DocumentId} in {IndexAlias}", id, indexAlias);
            return;
        }

        var chunkingOptions = new AITextChunkingOptions
        {
            MaxChunkSize = _options.Value.ChunkSize,
            ChunkOverlap = _options.Value.ChunkOverlap,
        };

        foreach (var cultureGroup in fieldsByCulture)
        {
            var culture = cultureGroup.Key;
            var text = ExtractTextFromFields(cultureGroup);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("No text content for document {DocumentId} culture {Culture} in {IndexAlias}, deleting existing chunks", id, culture ?? "invariant", indexAlias);
                await _vectorStore.DeleteAsync(indexAlias, documentId, culture);
                continue;
            }

            try
            {
                IReadOnlyList<AITextChunk> chunks = _textChunker.ChunkText(text, chunkingOptions);

                // Delete all existing chunks for this document+culture before inserting new ones
                await _vectorStore.DeleteAsync(indexAlias, documentId, culture);

                // Generate embeddings for all chunks in batch
                IEnumerable<string> chunkTexts = chunks.Select(c => c.Text);
                GeneratedEmbeddings<Embedding<float>> embeddings =
                    await _embeddingService.GenerateEmbeddingsAsync(chunkTexts);

                for (var i = 0; i < chunks.Count; i++)
                {
                    var metadata = new Dictionary<string, object>
                    {
                        ["objectType"] = objectType.ToString(),
                        ["chunkIndex"] = i,
                        ["totalChunks"] = chunks.Count,
                    };

                    await _vectorStore.UpsertAsync(indexAlias, documentId, culture, i, embeddings[i].Vector, metadata);
                }

                _logger.LogDebug("Indexed document {DocumentId} culture {Culture} in {IndexAlias} ({ChunkCount} chunks)", id, culture ?? "invariant", indexAlias, chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for document {DocumentId} culture {Culture} in {IndexAlias}", id, culture ?? "invariant", indexAlias);
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
    {
        foreach (Guid id in ids)
        {
            await _vectorStore.DeleteDocumentAsync(indexAlias, id.ToString("D"));
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(string indexAlias)
    {
        await _vectorStore.ResetAsync(indexAlias);
        _logger.LogInformation("Reset vector index {IndexAlias}", indexAlias);
    }

    /// <inheritdoc />
    public async Task<IndexMetadata> GetMetadataAsync(string indexAlias)
    {
        var count = await _vectorStore.GetDocumentCountAsync(indexAlias);
        return new IndexMetadata(count, HealthStatus.Healthy);
    }

    private static string ExtractTextFromFields(IEnumerable<IndexField> fields)
    {
        var parts = new List<string>();

        foreach (IndexField field in fields)
        {
            IndexValue value = field.Value;

            AppendTexts(parts, value.TextsR1);
            AppendTexts(parts, value.TextsR2);
            AppendTexts(parts, value.TextsR3);
            AppendTexts(parts, value.Texts);
        }

        return string.Join(" ", parts);
    }

    private static void AppendTexts(List<string> parts, IEnumerable<string>? texts)
    {
        if (texts is null)
        {
            return;
        }

        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var cleaned = AITextSanitizer.StripHtml(text);

            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                parts.Add(cleaned);
            }
        }
    }
}

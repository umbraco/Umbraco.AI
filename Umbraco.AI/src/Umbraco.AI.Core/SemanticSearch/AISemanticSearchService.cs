using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Provides semantic search using vector embeddings and cosine similarity.
/// </summary>
internal sealed class AISemanticSearchService : IAISemanticSearchService
{
    private readonly Dictionary<string, ISemanticIndexSource> _sources;
    private readonly IAIEmbeddingService _embeddingService;
    private readonly IAIContentEmbeddingRepository _repository;
    private readonly IAIProfileService _profileService;
    private readonly AISemanticSearchOptions _options;
    private readonly ILogger<AISemanticSearchService> _logger;

    public AISemanticSearchService(
        IEnumerable<ISemanticIndexSource> sources,
        IAIEmbeddingService embeddingService,
        IAIContentEmbeddingRepository repository,
        IAIProfileService profileService,
        IOptions<AISemanticSearchOptions> options,
        ILogger<AISemanticSearchService> logger)
    {
        _sources = sources.ToDictionary(s => s.EntityType, StringComparer.OrdinalIgnoreCase);
        _embeddingService = embeddingService;
        _repository = repository;
        _profileService = profileService;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        string query,
        SemanticSearchQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new SemanticSearchQueryOptions();

        // Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
        var queryVector = queryEmbedding.Vector.ToArray();

        // Load all stored embeddings
        var embeddings = await _repository.GetAllAsync(cancellationToken);

        // Compute similarity and filter
        var results = new List<(ContentEmbedding Embedding, float Similarity)>();

        foreach (var embedding in embeddings)
        {
            // Apply type filter
            if (options.TypeFilter is not null &&
                !string.Equals(embedding.ContentType, options.TypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Apply content type alias filter
            if (options.ContentTypeAliases is { Length: > 0 } &&
                !options.ContentTypeAliases.Contains(embedding.ContentTypeAlias, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var storedVector = VectorMath.DeserializeVector(embedding.Vector);
            var similarity = VectorMath.CosineSimilarity(queryVector, storedVector);

            if (similarity >= options.MinimumSimilarity)
            {
                results.Add((embedding, similarity));
            }
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(options.MaxResults)
            .Select(r => new SemanticSearchResult(
                r.Embedding.ContentKey,
                r.Embedding.Name,
                r.Embedding.ContentType,
                r.Embedding.ContentTypeAlias,
                r.Similarity,
                Truncate(r.Embedding.TextContent, 200)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task IndexEntityAsync(Guid entityKey, string entityType, CancellationToken cancellationToken = default)
    {
        if (!_sources.TryGetValue(entityType, out var source))
        {
            _logger.LogWarning("No semantic index source registered for entity type '{EntityType}'", entityType);
            return;
        }

        var entry = await source.GetEntryAsync(entityKey, cancellationToken);
        if (entry is null)
        {
            _logger.LogDebug("No embeddable text found for entity {EntityKey} of type '{EntityType}'", entityKey, entityType);
            return;
        }

        var profile = await GetEmbeddingProfileAsync(cancellationToken);
        var embedding = await _embeddingService.GenerateEmbeddingAsync(profile.Id, entry.Text, cancellationToken: cancellationToken);
        var vector = embedding.Vector.ToArray();

        var contentEmbedding = new ContentEmbedding
        {
            Id = Guid.NewGuid(),
            ContentKey = entry.EntityKey,
            ContentType = entry.EntityType,
            ContentTypeAlias = entry.EntityTypeAlias,
            Name = entry.Name,
            TextContent = entry.Text,
            Vector = VectorMath.SerializeVector(vector),
            Dimensions = vector.Length,
            ProfileId = profile.Id,
            ModelId = profile.Model.ModelId,
            DateIndexed = DateTime.UtcNow,
            ContentDateModified = entry.DateModified
        };

        await _repository.SaveAsync(contentEmbedding, cancellationToken);
        _logger.LogDebug("Indexed entity {EntityKey} of type '{EntityType}'", entityKey, entityType);
    }

    /// <inheritdoc />
    public async Task RemoveEntityAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteByContentKeyAsync(entityKey, cancellationToken);
        _logger.LogDebug("Removed embedding for entity {EntityKey}", entityKey);
    }

    /// <inheritdoc />
    public async Task ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetEmbeddingProfileAsync(cancellationToken);

        // Clear existing embeddings for this profile
        await _repository.DeleteByProfileIdAsync(profile.Id, cancellationToken);

        _logger.LogInformation("Starting full semantic reindex across {SourceCount} source(s)", _sources.Count);

        var totalIndexed = 0;

        foreach (var source in _sources.Values)
        {
            var batch = new List<(SemanticIndexEntry Entry, string Text)>();

            await foreach (var entry in source.GetAllEntriesAsync(cancellationToken))
            {
                batch.Add((entry, entry.Text));

                if (batch.Count >= _options.BatchSize)
                {
                    totalIndexed += await ProcessBatchAsync(batch, profile, cancellationToken);
                    batch.Clear();
                }
            }

            // Process remaining items
            if (batch.Count > 0)
            {
                totalIndexed += await ProcessBatchAsync(batch, profile, cancellationToken);
            }
        }

        _logger.LogInformation("Semantic reindex complete. Indexed {TotalIndexed} entities", totalIndexed);
    }

    /// <inheritdoc />
    public async Task<SemanticSearchIndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default)
    {
        var count = await _repository.GetCountAsync(cancellationToken);

        AIProfile? profile = null;
        try
        {
            profile = await GetEmbeddingProfileAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // No embedding profile configured
        }

        return new SemanticSearchIndexStatus(
            count,
            profile?.Id,
            profile?.Model.ModelId);
    }

    private async Task<int> ProcessBatchAsync(
        List<(SemanticIndexEntry Entry, string Text)> batch,
        AIProfile profile,
        CancellationToken cancellationToken)
    {
        var texts = batch.Select(b => b.Text).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(profile.Id, texts, cancellationToken: cancellationToken);

        var contentEmbeddings = new List<ContentEmbedding>();

        for (var i = 0; i < batch.Count; i++)
        {
            var entry = batch[i].Entry;
            var vector = embeddings[i].Vector.ToArray();

            contentEmbeddings.Add(new ContentEmbedding
            {
                Id = Guid.NewGuid(),
                ContentKey = entry.EntityKey,
                ContentType = entry.EntityType,
                ContentTypeAlias = entry.EntityTypeAlias,
                Name = entry.Name,
                TextContent = entry.Text,
                Vector = VectorMath.SerializeVector(vector),
                Dimensions = vector.Length,
                ProfileId = profile.Id,
                ModelId = profile.Model.ModelId,
                DateIndexed = DateTime.UtcNow,
                ContentDateModified = entry.DateModified
            });
        }

        await _repository.SaveBatchAsync(contentEmbeddings, cancellationToken);
        return contentEmbeddings.Count;
    }

    private async Task<AIProfile> GetEmbeddingProfileAsync(CancellationToken cancellationToken)
    {
        return await _profileService.GetDefaultProfileAsync(AICapability.Embedding, cancellationToken);
    }

    private static string? Truncate(string? text, int maxLength)
    {
        if (text is null || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + "...";
    }
}

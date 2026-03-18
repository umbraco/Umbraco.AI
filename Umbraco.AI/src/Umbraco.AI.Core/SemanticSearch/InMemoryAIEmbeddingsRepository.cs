using System.Collections.Concurrent;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// In-memory implementation of the embeddings repository.
/// Replaced by EF Core implementation when persistence is registered.
/// </summary>
internal sealed class InMemoryAIEmbeddingsRepository : IAIEmbeddingsRepository
{
    private readonly ConcurrentDictionary<Guid, AIEmbedding> _embeddings = new();

    /// <inheritdoc />
    public Task<AIEmbedding?> GetByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        var embedding = _embeddings.Values.FirstOrDefault(e => e.EntityKey == entityKey);
        return Task.FromResult(embedding);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIEmbedding>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AIEmbedding> result = _embeddings.Values.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIEmbedding>> GetByFilterAsync(
        string? entityType = null,
        string[]? entityTypeAliases = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIEmbedding> query = _embeddings.Values;

        if (entityType is not null)
        {
            query = query.Where(e => string.Equals(e.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
        }

        if (entityTypeAliases is { Length: > 0 })
        {
            query = query.Where(e => entityTypeAliases.Contains(e.EntityTypeAlias, StringComparer.OrdinalIgnoreCase));
        }

        IReadOnlyList<AIEmbedding> result = query.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIEmbedding>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AIEmbedding> result = _embeddings.Values
            .Where(e => e.ProfileId == profileId)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SaveAsync(AIEmbedding embedding, CancellationToken cancellationToken = default)
    {
        // Upsert by entity key: find existing by entity key and remove it first
        var existing = _embeddings.Values.FirstOrDefault(e => e.EntityKey == embedding.EntityKey);
        if (existing != null)
        {
            _embeddings.TryRemove(existing.Id, out _);
        }

        _embeddings[embedding.Id] = embedding;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(IEnumerable<AIEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        foreach (var embedding in embeddings)
        {
            await SaveAsync(embedding, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task DeleteByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        var toRemove = _embeddings.Values.FirstOrDefault(e => e.EntityKey == entityKey);
        if (toRemove != null)
        {
            _embeddings.TryRemove(toRemove.Id, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var toRemove = _embeddings.Values.Where(e => e.ProfileId == profileId).ToList();
        foreach (var embedding in toRemove)
        {
            _embeddings.TryRemove(embedding.Id, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_embeddings.Count);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EmbeddingSimilarityResult>> SearchByVectorAsync(
        float[] queryVector,
        string? entityType = null,
        string[]? entityTypeAliases = null,
        float minimumSimilarity = 0.5f,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIEmbedding> query = _embeddings.Values;

        if (entityType is not null)
        {
            query = query.Where(e => string.Equals(e.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
        }

        if (entityTypeAliases is { Length: > 0 })
        {
            query = query.Where(e => entityTypeAliases.Contains(e.EntityTypeAlias, StringComparer.OrdinalIgnoreCase));
        }

        IReadOnlyList<EmbeddingSimilarityResult> results = query
            .Select(e => new EmbeddingSimilarityResult(e, VectorMath.CosineSimilarity(queryVector, VectorMath.DeserializeVector(e.Vector))))
            .Where(r => r.SimilarityScore >= minimumSimilarity)
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();

        return Task.FromResult(results);
    }
}

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
    public Task<AIEmbedding?> GetByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        var embedding = _embeddings.Values.FirstOrDefault(e => e.ContentKey == contentKey);
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
        string? contentType = null,
        string[]? contentTypeAliases = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIEmbedding> query = _embeddings.Values;

        if (contentType is not null)
        {
            query = query.Where(e => string.Equals(e.ContentType, contentType, StringComparison.OrdinalIgnoreCase));
        }

        if (contentTypeAliases is { Length: > 0 })
        {
            query = query.Where(e => contentTypeAliases.Contains(e.ContentTypeAlias, StringComparer.OrdinalIgnoreCase));
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
        // Upsert by content key: find existing by content key and remove it first
        var existing = _embeddings.Values.FirstOrDefault(e => e.ContentKey == embedding.ContentKey);
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
    public Task DeleteByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        var toRemove = _embeddings.Values.FirstOrDefault(e => e.ContentKey == contentKey);
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
}

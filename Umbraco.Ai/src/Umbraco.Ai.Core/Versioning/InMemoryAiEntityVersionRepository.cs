using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// In-memory implementation of <see cref="IAiEntityVersionRepository"/> for testing.
/// </summary>
internal sealed class InMemoryAiEntityVersionRepository : IAiEntityVersionRepository
{
    private readonly ConcurrentDictionary<string, AiEntityVersion> _versions = new();

    private static string GetKey(Guid entityId, string entityType, int version)
        => $"{entityType}:{entityId}:{version}";

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var versions = _versions.Values
            .Where(v => v.EntityId == entityId && v.EntityType == entityType)
            .OrderByDescending(v => v.Version)
            .ToList();

        if (limit.HasValue)
        {
            versions = versions.Take(limit.Value).ToList();
        }

        return Task.FromResult<IEnumerable<AiEntityVersion>>(versions);
    }

    /// <inheritdoc />
    public Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(entityId, entityType, version);
        _versions.TryGetValue(key, out var result);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        int? userId,
        string? changeDescription,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(entityId, entityType, version);
        var versionEntity = new AiEntityVersion
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = entityType,
            Version = version,
            Snapshot = snapshot,
            DateCreated = DateTime.UtcNow,
            CreatedByUserId = userId,
            ChangeDescription = changeDescription
        };

        _versions[key] = versionEntity;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var keysToRemove = _versions.Keys
            .Where(k => k.StartsWith($"{entityType}:{entityId}:"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _versions.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}

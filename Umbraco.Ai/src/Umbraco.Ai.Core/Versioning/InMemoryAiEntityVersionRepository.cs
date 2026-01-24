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
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var versions = _versions.Values
            .Where(v => v.EntityId == entityId && v.EntityType == entityType)
            .OrderByDescending(v => v.Version)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IEnumerable<AiEntityVersion>>(versions);
    }

    /// <inheritdoc />
    public Task<int> GetVersionCountByEntityAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var count = _versions.Values
            .Count(v => v.EntityId == entityId && v.EntityType == entityType);

        return Task.FromResult(count);
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
        Guid? userId,
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

    /// <inheritdoc />
    public Task<int> DeleteVersionsOlderThanAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default)
    {
        var keysToRemove = _versions
            .Where(kvp => kvp.Value.DateCreated < threshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _versions.TryRemove(key, out _);
        }

        return Task.FromResult(keysToRemove.Count);
    }

    /// <inheritdoc />
    public Task<int> DeleteExcessVersionsAsync(
        int maxVersionsPerEntity,
        CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;

        // Group versions by (EntityId, EntityType)
        var groupedVersions = _versions.Values
            .GroupBy(v => new { v.EntityId, v.EntityType })
            .ToList();

        foreach (var group in groupedVersions)
        {
            // Get versions to delete (all except the most recent N)
            var versionsToDelete = group
                .OrderByDescending(v => v.Version)
                .Skip(maxVersionsPerEntity)
                .ToList();

            foreach (var version in versionsToDelete)
            {
                var key = GetKey(version.EntityId, version.EntityType, version.Version);
                if (_versions.TryRemove(key, out _))
                {
                    deletedCount++;
                }
            }
        }

        return Task.FromResult(deletedCount);
    }

    /// <inheritdoc />
    public Task<int> GetVersionCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_versions.Count);
    }
}

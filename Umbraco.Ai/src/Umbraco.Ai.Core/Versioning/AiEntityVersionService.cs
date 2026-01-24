using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Default implementation of <see cref="IAiEntityVersionService"/>.
/// </summary>
internal sealed class AiEntityVersionService : IAiEntityVersionService
{
    private readonly IAiEntityVersionRepository _repository;
    private readonly AiVersionableEntityAdapterCollection _entityTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiEntityVersionService"/> class.
    /// </summary>
    /// <param name="repository">The version repository.</param>
    /// <param name="entityTypes">The collection of versionable entity type handlers.</param>
    public AiEntityVersionService(
        IAiEntityVersionRepository repository,
        AiVersionableEntityAdapterCollection entityTypes)
    {
        _repository = repository;
        _entityTypes = entityTypes;
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        ValidateEntityType(entityType);
        return _repository.GetVersionHistoryAsync(entityId, entityType, limit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        var handler = _entityTypes.GetByTypeName(entityType);
        if (handler is null)
        {
            throw new ArgumentException($"Unknown entity type: {entityType}", nameof(entityType));
        }

        // First try to get from repository (historical versions)
        var versionRecord = await _repository.GetVersionAsync(entityId, entityType, version, cancellationToken);
        if (versionRecord is not null)
        {
            return versionRecord;
        }

        // If not found in repository, check if it's the current version
        var currentEntity = await handler.GetEntityAsync(entityId, cancellationToken);
        if (currentEntity is IAiVersionableEntity versionable && versionable.Version == version)
        {
            // Return a synthetic version record for the current state
            return new AiEntityVersion
            {
                Id = Guid.Empty, // Synthetic - no DB record exists
                EntityId = entityId,
                EntityType = entityType,
                Version = version,
                Snapshot = handler.CreateSnapshot(currentEntity),
                DateCreated = GetDateModified(currentEntity) ?? DateTime.UtcNow,
                CreatedByUserId = GetModifiedByUserId(currentEntity)
            };
        }

        return null;
    }

    private static DateTime? GetDateModified(object entity)
    {
        var property = entity.GetType().GetProperty("DateModified");
        return property?.GetValue(entity) as DateTime?;
    }

    private static Guid? GetModifiedByUserId(object entity)
    {
        var property = entity.GetType().GetProperty("ModifiedByUserId");
        return property?.GetValue(entity) as Guid?;
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetVersionSnapshotAsync<TEntity>(
        Guid entityId,
        int version,
        CancellationToken cancellationToken = default)
        where TEntity : class, IAiVersionableEntity
    {
        var handler = GetHandler<TEntity>();

        // First try to get from repository (historical versions)
        var versionRecord = await _repository.GetVersionAsync(entityId, handler.EntityTypeName, version, cancellationToken);
        if (versionRecord is not null)
        {
            return handler.RestoreFromSnapshot(versionRecord.Snapshot) as TEntity;
        }

        // If not found in repository, check if it's the current version
        var currentEntity = await handler.GetEntityAsync(entityId, cancellationToken);
        if (currentEntity is TEntity typedEntity && typedEntity.Version == version)
        {
            return typedEntity;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task SaveVersionAsync<TEntity>(
        TEntity entity,
        Guid? userId,
        string? changeDescription = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, IAiVersionableEntity
    {
        var handler = GetHandler<TEntity>();

        // Get entity ID via reflection (all versionable entities have an Id property)
        var idProperty = typeof(TEntity).GetProperty("Id")
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have an Id property");
        var entityId = (Guid)idProperty.GetValue(entity)!;

        var snapshot = handler.CreateSnapshot(entity);

        await _repository.SaveVersionAsync(
            entityId,
            handler.EntityTypeName,
            entity.Version,
            snapshot,
            userId,
            changeDescription,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        Guid? userId,
        string? changeDescription = null,
        CancellationToken cancellationToken = default)
    {
        ValidateEntityType(entityType);
        return _repository.SaveVersionAsync(entityId, entityType, version, snapshot, userId, changeDescription, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        ValidateEntityType(entityType);
        return _repository.DeleteVersionsAsync(entityId, entityType, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiVersionComparison?> CompareVersionsAsync(
        Guid entityId,
        string entityType,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        var handler = _entityTypes.GetByTypeName(entityType);
        if (handler is null)
        {
            throw new ArgumentException($"Unknown entity type: {entityType}", nameof(entityType));
        }

        // Get the current entity to check its version
        var currentEntity = await handler.GetEntityAsync(entityId, cancellationToken);
        var currentVersion = (currentEntity as IAiVersionableEntity)?.Version ?? 0;

        // Resolve "from" version - use current entity if it matches the requested version
        object? fromEntity;
        if (fromVersion == currentVersion && currentEntity is not null)
        {
            fromEntity = currentEntity;
        }
        else
        {
            var fromVersionRecord = await _repository.GetVersionAsync(entityId, entityType, fromVersion, cancellationToken);
            fromEntity = fromVersionRecord is null ? null : handler.RestoreFromSnapshot(fromVersionRecord.Snapshot);
        }

        // Resolve "to" version - use current entity if it matches the requested version
        object? toEntity;
        if (toVersion == currentVersion && currentEntity is not null)
        {
            toEntity = currentEntity;
        }
        else
        {
            var toVersionRecord = await _repository.GetVersionAsync(entityId, entityType, toVersion, cancellationToken);
            toEntity = toVersionRecord is null ? null : handler.RestoreFromSnapshot(toVersionRecord.Snapshot);
        }

        if (fromEntity is null || toEntity is null)
        {
            return null;
        }

        var changes = handler.CompareVersions(fromEntity, toEntity);

        return new AiVersionComparison(entityId, entityType, fromVersion, toVersion, changes);
    }

    /// <inheritdoc />
    public string CreateSnapshot<TEntity>(TEntity entity)
        where TEntity : class, IAiVersionableEntity
    {
        var handler = GetHandler<TEntity>();
        return handler.CreateSnapshot(entity);
    }

    /// <inheritdoc />
    public TEntity? RestoreFromSnapshot<TEntity>(string snapshot)
        where TEntity : class, IAiVersionableEntity
    {
        var handler = GetHandler<TEntity>();
        return handler.RestoreFromSnapshot(snapshot) as TEntity;
    }

    private IAiVersionableEntityAdapter GetHandler<TEntity>()
        where TEntity : class, IAiVersionableEntity
    {
        var handler = _entityTypes.GetByType<TEntity>();
        if (handler is null)
        {
            throw new InvalidOperationException($"No versionable entity type handler registered for {typeof(TEntity).Name}");
        }

        return handler;
    }

    private void ValidateEntityType(string entityType)
    {
        if (_entityTypes.GetByTypeName(entityType) is null)
        {
            throw new ArgumentException(
                $"Unknown entity type: {entityType}. Supported types: {string.Join(", ", _entityTypes.GetSupportedEntityTypes())}",
                nameof(entityType));
        }
    }
}

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
    public Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        ValidateEntityType(entityType);
        return _repository.GetVersionAsync(entityId, entityType, version, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetVersionSnapshotAsync<TEntity>(
        Guid entityId,
        int version,
        CancellationToken cancellationToken = default)
        where TEntity : class, IAiVersionableEntity
    {
        var handler = GetHandler<TEntity>();
        var versionRecord = await _repository.GetVersionAsync(entityId, handler.EntityTypeName, version, cancellationToken);

        if (versionRecord is null)
        {
            return null;
        }

        return handler.RestoreFromSnapshot(versionRecord.Snapshot) as TEntity;
    }

    /// <inheritdoc />
    public async Task SaveVersionAsync<TEntity>(
        TEntity entity,
        int? userId,
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
        int? userId,
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

        var fromVersionRecord = await _repository.GetVersionAsync(entityId, entityType, fromVersion, cancellationToken);
        var toVersionRecord = await _repository.GetVersionAsync(entityId, entityType, toVersion, cancellationToken);

        if (fromVersionRecord is null || toVersionRecord is null)
        {
            return null;
        }

        var fromEntity = handler.RestoreFromSnapshot(fromVersionRecord.Snapshot);
        var toEntity = handler.RestoreFromSnapshot(toVersionRecord.Snapshot);

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

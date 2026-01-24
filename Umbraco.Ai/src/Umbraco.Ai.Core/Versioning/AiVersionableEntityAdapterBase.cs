namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Base class for versionable entity type handlers that provides strongly-typed methods.
/// </summary>
/// <typeparam name="TEntity">The entity type this handler manages.</typeparam>
/// <remarks>
/// <para>
/// This base class simplifies implementing <see cref="IAiVersionableEntityAdapter"/> by:
/// </para>
/// <list type="bullet">
///   <item>Automatically deriving <see cref="EntityTypeName"/> from the generic type parameter</item>
///   <item>Providing strongly-typed abstract methods for implementations</item>
///   <item>Handling the object-to-typed casting in the interface implementation</item>
/// </list>
/// </remarks>
public abstract class AiVersionableEntityAdapterBase<TEntity> : IAiVersionableEntityAdapter
    where TEntity : class, IAiVersionableEntity
{
    /// <inheritdoc />
    /// <remarks>
    /// Derived from the generic type parameter by removing the "Ai" prefix.
    /// For example: <c>AiConnection</c> becomes <c>Connection</c>.
    /// </remarks>
    public virtual string EntityTypeName
    {
        get
        {
            var name = typeof(TEntity).Name;
            return name.StartsWith("Ai", StringComparison.Ordinal)
                ? name.Substring(2)
                : name;
        }
    }

    /// <inheritdoc />
    public Type EntityType => typeof(TEntity);

    /// <inheritdoc />
    string IAiVersionableEntityAdapter.CreateSnapshot(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is not TEntity typed)
        {
            throw new ArgumentException($"Expected entity of type {typeof(TEntity).Name} but got {entity.GetType().Name}", nameof(entity));
        }

        return CreateSnapshot(typed);
    }

    /// <inheritdoc />
    object? IAiVersionableEntityAdapter.RestoreFromSnapshot(string json)
        => RestoreFromSnapshot(json);

    /// <inheritdoc />
    public IReadOnlyList<AiPropertyChange> CompareVersions(object from, object to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (from is not TEntity typedFrom)
        {
            throw new ArgumentException($"Expected entity of type {typeof(TEntity).Name} but got {from.GetType().Name}", nameof(from));
        }

        if (to is not TEntity typedTo)
        {
            throw new ArgumentException($"Expected entity of type {typeof(TEntity).Name} but got {to.GetType().Name}", nameof(to));
        }

        return CompareVersions(typedFrom, typedTo);
    }

    /// <summary>
    /// Creates a JSON snapshot of the entity for version storage.
    /// </summary>
    /// <param name="entity">The entity to snapshot.</param>
    /// <returns>JSON string representing the entity state.</returns>
    protected abstract string CreateSnapshot(TEntity entity);

    /// <summary>
    /// Restores an entity from a JSON snapshot.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The restored entity, or null if restoration fails.</returns>
    protected abstract TEntity? RestoreFromSnapshot(string json);

    /// <summary>
    /// Compares two entity versions and returns the list of property changes.
    /// </summary>
    /// <param name="from">The older entity version.</param>
    /// <param name="to">The newer entity version.</param>
    /// <returns>A list of property changes between the versions.</returns>
    protected abstract IReadOnlyList<AiPropertyChange> CompareVersions(TEntity from, TEntity to);

    /// <inheritdoc />
    public abstract Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public async Task<object?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await GetEntityCoreAsync(entityId, cancellationToken);
    }

    /// <summary>
    /// Gets the current state of an entity from the main entity table.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    protected abstract Task<TEntity?> GetEntityCoreAsync(Guid entityId, CancellationToken cancellationToken);
}

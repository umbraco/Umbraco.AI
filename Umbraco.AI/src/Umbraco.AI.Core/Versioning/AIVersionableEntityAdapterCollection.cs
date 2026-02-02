using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// A collection of versionable entity adapters.
/// </summary>
/// <remarks>
/// This collection provides lookup methods to find the appropriate adapter for a given entity type.
/// </remarks>
public sealed class AIVersionableEntityAdapterCollection : BuilderCollectionBase<IAiVersionableEntityAdapter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIVersionableEntityAdapterCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the entity adapters.</param>
    public AIVersionableEntityAdapterCollection(Func<IEnumerable<IAiVersionableEntityAdapter>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a versionable entity adapter by its type name.
    /// </summary>
    /// <param name="entityTypeName">The entity type name (e.g., "Connection", "Profile").</param>
    /// <returns>The adapter, or <c>null</c> if not found.</returns>
    public IAiVersionableEntityAdapter? GetByTypeName(string entityTypeName)
        => this.FirstOrDefault(t => t.EntityTypeName.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a versionable entity adapter by its CLR type.
    /// </summary>
    /// <param name="entityType">The CLR type of the entity.</param>
    /// <returns>The adapter, or <c>null</c> if not found.</returns>
    public IAiVersionableEntityAdapter? GetByType(Type entityType)
        => this.FirstOrDefault(t => t.EntityType == entityType);

    /// <summary>
    /// Gets a versionable entity adapter by its CLR type.
    /// </summary>
    /// <typeparam name="TEntity">The CLR type of the entity.</typeparam>
    /// <returns>The adapter, or <c>null</c> if not found.</returns>
    public IAiVersionableEntityAdapter? GetByType<TEntity>()
        => GetByType(typeof(TEntity));

    /// <summary>
    /// Gets all supported entity type names.
    /// </summary>
    /// <returns>A collection of entity type names.</returns>
    public IEnumerable<string> GetSupportedEntityTypes()
        => this.Select(t => t.EntityTypeName);
}

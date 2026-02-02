namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Represents the result of comparing two versions of an entity.
/// </summary>
public sealed class AIVersionComparison
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIVersionComparison"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the entity being compared.</param>
    /// <param name="entityType">The type of the entity being compared.</param>
    /// <param name="fromVersion">The older version number.</param>
    /// <param name="toVersion">The newer version number.</param>
    /// <param name="changes">The list of property changes.</param>
    public AIVersionComparison(
        Guid entityId,
        string entityType,
        int fromVersion,
        int toVersion,
        IReadOnlyList<AIPropertyChange> changes)
    {
        EntityId = entityId;
        EntityType = entityType;
        FromVersion = fromVersion;
        ToVersion = toVersion;
        Changes = changes;
    }

    /// <summary>
    /// Gets the ID of the entity being compared.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the type of the entity being compared.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the older version number.
    /// </summary>
    public int FromVersion { get; }

    /// <summary>
    /// Gets the newer version number.
    /// </summary>
    public int ToVersion { get; }

    /// <summary>
    /// Gets the list of property changes between the versions.
    /// </summary>
    public IReadOnlyList<AIPropertyChange> Changes { get; }
}

using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Collection of entity adapters.
/// Adapters are selected based on entity type, with fallback to the default adapter.
/// </summary>
public sealed class AIEntityAdapterCollection : BuilderCollectionBase<IAIEntityAdapter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIEntityAdapterCollection"/> class.
    /// </summary>
    /// <param name="items">The adapters.</param>
    public AIEntityAdapterCollection(Func<IEnumerable<IAIEntityAdapter>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Gets the adapter for the specified entity type.
    /// Returns the default adapter (EntityType = null) if no specific adapter is found.
    /// </summary>
    /// <param name="entityType">The entity type to find an adapter for.</param>
    /// <returns>The adapter for the entity type, or the default adapter if not found.</returns>
    public IAIEntityAdapter GetAdapter(string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        // Try to find entity-type-specific adapter (case-insensitive)
        var adapter = this.FirstOrDefault(a =>
            a.EntityType != null &&
            string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

        // Fall back to default adapter (EntityType = null)
        adapter ??= this.FirstOrDefault(a => a.EntityType == null);

        // Should never happen if GenericEntityAdapter is registered
        if (adapter == null)
        {
            throw new InvalidOperationException(
                "No default entity adapter found. Ensure GenericEntityAdapter is registered.");
        }

        return adapter;
    }

    /// <summary>
    /// Gets all adapters that handle specific entity types (excludes the default/fallback adapter).
    /// </summary>
    /// <returns>Adapters with a non-null EntityType.</returns>
    public IEnumerable<IAIEntityAdapter> GetEntityTypeAdapters()
        => this.Where(a => a.EntityType != null);
}

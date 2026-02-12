using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Collection of entity formatters.
/// Formatters are selected based on entity type, with fallback to the default formatter.
/// </summary>
public sealed class AIEntityFormatterCollection : BuilderCollectionBase<IAIEntityFormatter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIEntityFormatterCollection"/> class.
    /// </summary>
    /// <param name="items">The formatters.</param>
    public AIEntityFormatterCollection(Func<IEnumerable<IAIEntityFormatter>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Gets the formatter for the specified entity type.
    /// Returns the default formatter (EntityType = null) if no specific formatter is found.
    /// </summary>
    /// <param name="entityType">The entity type to find a formatter for.</param>
    /// <returns>The formatter for the entity type, or the default formatter if not found.</returns>
    public IAIEntityFormatter GetFormatter(string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        // Try to find entity-type-specific formatter (case-insensitive)
        var formatter = this.FirstOrDefault(f =>
            f.EntityType != null &&
            string.Equals(f.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

        // Fall back to default formatter (EntityType = null)
        formatter ??= this.FirstOrDefault(f => f.EntityType == null);

        // Should never happen if AIGenericEntityFormatter is registered
        if (formatter == null)
        {
            throw new InvalidOperationException(
                "No default entity formatter found. Ensure AIGenericEntityFormatter is registered.");
        }

        return formatter;
    }
}

using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Collection of registered test features.
/// Test features are discovered via the [AiTestFeature] attribute and registered
/// automatically during startup.
/// </summary>
public class AiTestFeatureCollection : BuilderCollectionBase<IAiTestFeature>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiTestFeatureCollection"/> class.
    /// </summary>
    /// <param name="items">The test features to include in the collection.</param>
    public AiTestFeatureCollection(Func<IEnumerable<IAiTestFeature>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Gets a test feature by its unique identifier.
    /// </summary>
    /// <param name="id">The test feature ID (e.g., "prompt", "agent").</param>
    /// <returns>The test feature, or null if not found.</returns>
    public IAiTestFeature? GetById(string id)
    {
        return this.FirstOrDefault(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}

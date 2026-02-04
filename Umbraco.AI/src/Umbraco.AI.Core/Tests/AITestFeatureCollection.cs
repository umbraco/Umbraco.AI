using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// A collection of AI test features (harnesses).
/// </summary>
public sealed class AITestFeatureCollection : BuilderCollectionBase<IAITestFeature>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestFeatureCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the test features.</param>
    public AITestFeatureCollection(Func<IEnumerable<IAITestFeature>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a test feature by its unique identifier.
    /// </summary>
    /// <param name="testTypeId">The test feature identifier.</param>
    /// <returns>The test feature, or <c>null</c> if not found.</returns>
    public IAITestFeature? GetById(string testTypeId)
        => this.FirstOrDefault(f => f.Id.Equals(testTypeId, StringComparison.OrdinalIgnoreCase));
}

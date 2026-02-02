using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// A collection of AI context resource types.
/// </summary>
public sealed class AIContextResourceTypeCollection : BuilderCollectionBase<IAIContextResourceType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the resource types.</param>
    public AIContextResourceTypeCollection(Func<IEnumerable<IAIContextResourceType>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a resource type by its unique identifier.
    /// </summary>
    /// <param name="id">The resource type identifier (e.g., "brand-voice", "text").</param>
    /// <returns>The resource type, or <c>null</c> if not found.</returns>
    public IAIContextResourceType? GetById(string id)
        => this.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

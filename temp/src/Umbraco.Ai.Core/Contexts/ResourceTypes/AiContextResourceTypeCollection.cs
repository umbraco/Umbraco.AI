using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Contexts.ResourceTypes;

/// <summary>
/// A collection of AI context resource types.
/// </summary>
public sealed class AiContextResourceTypeCollection : BuilderCollectionBase<IAiContextResourceType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResourceTypeCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the resource types.</param>
    public AiContextResourceTypeCollection(Func<IEnumerable<IAiContextResourceType>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a resource type by its unique identifier.
    /// </summary>
    /// <param name="id">The resource type identifier (e.g., "brand-voice", "text").</param>
    /// <returns>The resource type, or <c>null</c> if not found.</returns>
    public IAiContextResourceType? GetById(string id)
        => this.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

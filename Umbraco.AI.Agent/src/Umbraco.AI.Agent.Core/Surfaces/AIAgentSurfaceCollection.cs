using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Surfaces;

/// <summary>
/// A collection of AI agent surfaces.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides lookup methods to find surfaces by ID.
/// Surfaces are auto-discovered via the <see cref="AIAgentSurfaceAttribute"/>.
/// </para>
/// </remarks>
public sealed class AIAgentSurfaceCollection : BuilderCollectionBase<IAIAgentSurface>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSurfaceCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the surfaces.</param>
    public AIAgentSurfaceCollection(Func<IEnumerable<IAIAgentSurface>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a surface by its unique identifier.
    /// </summary>
    /// <param name="surfaceId">The surface identifier.</param>
    /// <returns>The surface, or <c>null</c> if not found.</returns>
    public IAIAgentSurface? GetById(string surfaceId)
        => this.FirstOrDefault(s => s.Id.Equals(surfaceId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets multiple surfaces by their identifiers.
    /// </summary>
    /// <param name="surfaceIds">The surface identifiers.</param>
    /// <returns>The matching surfaces (unmatched IDs are ignored).</returns>
    public IEnumerable<IAIAgentSurface> GetByIds(IEnumerable<string> surfaceIds)
    {
        var ids = surfaceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return this.Where(s => ids.Contains(s.Id));
    }

    /// <summary>
    /// Checks if a surface with the given ID exists.
    /// </summary>
    /// <param name="surfaceId">The surface identifier.</param>
    /// <returns><c>true</c> if the surface exists; otherwise, <c>false</c>.</returns>
    public bool Exists(string surfaceId)
        => this.Any(s => s.Id.Equals(surfaceId, StringComparison.OrdinalIgnoreCase));
}

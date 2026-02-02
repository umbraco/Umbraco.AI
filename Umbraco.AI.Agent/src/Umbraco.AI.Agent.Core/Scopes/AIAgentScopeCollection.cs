using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Scopes;

/// <summary>
/// A collection of AI agent scopes.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides lookup methods to find scopes by ID.
/// Scopes are auto-discovered via the <see cref="AIAgentScopeAttribute"/>.
/// </para>
/// </remarks>
public sealed class AIAgentScopeCollection : BuilderCollectionBase<IAIAgentScope>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentScopeCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the scopes.</param>
    public AIAgentScopeCollection(Func<IEnumerable<IAIAgentScope>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a scope by its unique identifier.
    /// </summary>
    /// <param name="scopeId">The scope identifier.</param>
    /// <returns>The scope, or <c>null</c> if not found.</returns>
    public IAIAgentScope? GetById(string scopeId)
        => this.FirstOrDefault(s => s.Id.Equals(scopeId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets multiple scopes by their identifiers.
    /// </summary>
    /// <param name="scopeIds">The scope identifiers.</param>
    /// <returns>The matching scopes (unmatched IDs are ignored).</returns>
    public IEnumerable<IAIAgentScope> GetByIds(IEnumerable<string> scopeIds)
    {
        var ids = scopeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return this.Where(s => ids.Contains(s.Id));
    }

    /// <summary>
    /// Checks if a scope with the given ID exists.
    /// </summary>
    /// <param name="scopeId">The scope identifier.</param>
    /// <returns><c>true</c> if the scope exists; otherwise, <c>false</c>.</returns>
    public bool Exists(string scopeId)
        => this.Any(s => s.Id.Equals(scopeId, StringComparison.OrdinalIgnoreCase));
}

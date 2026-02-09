using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// A collection of AI tool scopes.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides lookup methods to find scopes by ID or domain.
/// Scopes are auto-discovered via the <see cref="AIToolScopeAttribute"/>.
/// </para>
/// </remarks>
public sealed class AIToolScopeCollection : BuilderCollectionBase<IAIToolScope>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIToolScopeCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the scopes.</param>
    public AIToolScopeCollection(Func<IEnumerable<IAIToolScope>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a scope by its unique identifier.
    /// </summary>
    /// <param name="scopeId">The scope identifier.</param>
    /// <returns>The scope, or <c>null</c> if not found.</returns>
    public IAIToolScope? GetById(string scopeId)
        => this.FirstOrDefault(s => s.Id.Equals(scopeId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets multiple scopes by their identifiers.
    /// </summary>
    /// <param name="scopeIds">The scope identifiers.</param>
    /// <returns>The matching scopes (unmatched IDs are ignored).</returns>
    public IEnumerable<IAIToolScope> GetByIds(IEnumerable<string> scopeIds)
    {
        var ids = scopeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return this.Where(s => ids.Contains(s.Id));
    }

    /// <summary>
    /// Gets all scopes in a specific domain.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>Scopes in the specified domain.</returns>
    public IEnumerable<IAIToolScope> GetByDomain(string domain)
        => this.Where(s => s.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Checks if a scope with the given ID exists.
    /// </summary>
    /// <param name="scopeId">The scope identifier.</param>
    /// <returns><c>true</c> if the scope exists; otherwise, <c>false</c>.</returns>
    public bool Exists(string scopeId)
        => this.Any(s => s.Id.Equals(scopeId, StringComparison.OrdinalIgnoreCase));
}

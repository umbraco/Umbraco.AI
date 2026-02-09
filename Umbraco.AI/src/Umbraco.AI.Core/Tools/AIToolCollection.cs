using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// A collection of AI tools.
/// </summary>
public sealed class AIToolCollection : BuilderCollectionBase<IAITool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIToolCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the tools.</param>
    public AIToolCollection(Func<IEnumerable<IAITool>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a tool by its unique identifier.
    /// </summary>
    /// <param name="toolId">The tool identifier.</param>
    /// <returns>The tool, or <c>null</c> if not found.</returns>
    public IAITool? GetById(string toolId)
        => this.FirstOrDefault(t => t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all tools in a specific scope.
    /// </summary>
    /// <param name="scopeId">The scope identifier to filter by.</param>
    /// <returns>Tools in the specified scope.</returns>
    public IEnumerable<IAITool> GetByScope(string scopeId)
        => this.Where(t => t.ScopeId.Equals(scopeId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all tools matching any of the specified scope identifiers.
    /// </summary>
    /// <param name="scopeIds">The scope identifiers to filter by.</param>
    /// <returns>Tools matching any of the specified scopes.</returns>
    public IEnumerable<IAITool> GetByScopes(IEnumerable<string> scopeIds)
    {
        var ids = scopeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return this.Where(t => ids.Contains(t.ScopeId));
    }

    /// <summary>
    /// Gets all tools with a specific tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>Tools with the specified tag.</returns>
    public IEnumerable<IAITool> GetWithTag(string tag)
        => this.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all destructive tools.
    /// </summary>
    /// <returns>Tools marked as destructive.</returns>
    public IEnumerable<IAITool> GetDestructive()
        => this.Where(t => t.IsDestructive);

    /// <summary>
    /// Gets all non-destructive tools.
    /// </summary>
    /// <returns>Tools not marked as destructive.</returns>
    public IEnumerable<IAITool> GetNonDestructive()
        => this.Where(t => !t.IsDestructive);

    /// <summary>
    /// Gets all system tools (tools that are always included in agent requests).
    /// </summary>
    /// <returns>System tools that cannot be removed or configured.</returns>
    public IEnumerable<IAITool> GetSystemTools()
        => this.Where(t => t is IAISystemTool);

    /// <summary>
    /// Gets all user tools (non-system tools that can be configured).
    /// </summary>
    /// <returns>User-configurable tools.</returns>
    public IEnumerable<IAITool> GetUserTools()
        => this.Where(t => t is not IAISystemTool);
}

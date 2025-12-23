using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tools;

/// <summary>
/// A collection of AI tools.
/// </summary>
public sealed class AiToolCollection : BuilderCollectionBase<IAiTool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiToolCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the tools.</param>
    public AiToolCollection(Func<IEnumerable<IAiTool>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a tool by its unique identifier.
    /// </summary>
    /// <param name="toolId">The tool identifier.</param>
    /// <returns>The tool, or <c>null</c> if not found.</returns>
    public IAiTool? GetById(string toolId)
        => this.FirstOrDefault(t => t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all tools in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Tools in the specified category.</returns>
    public IEnumerable<IAiTool> GetByCategory(string category)
        => this.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all tools with a specific tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>Tools with the specified tag.</returns>
    public IEnumerable<IAiTool> GetWithTag(string tag)
        => this.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all destructive tools.
    /// </summary>
    /// <returns>Tools marked as destructive.</returns>
    public IEnumerable<IAiTool> GetDestructive()
        => this.Where(t => t.IsDestructive);

    /// <summary>
    /// Gets all non-destructive tools.
    /// </summary>
    /// <returns>Tools not marked as destructive.</returns>
    public IEnumerable<IAiTool> GetNonDestructive()
        => this.Where(t => !t.IsDestructive);
}

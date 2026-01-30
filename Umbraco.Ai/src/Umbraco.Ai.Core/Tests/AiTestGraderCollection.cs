using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Collection of registered test graders.
/// Graders are discovered via the [AiTestGrader] attribute and registered
/// automatically during startup.
/// </summary>
public class AiTestGraderCollection : BuilderCollectionBase<IAiTestGrader>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiTestGraderCollection"/> class.
    /// </summary>
    /// <param name="items">The graders to include in the collection.</param>
    public AiTestGraderCollection(Func<IEnumerable<IAiTestGrader>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Gets a grader by its unique identifier.
    /// </summary>
    /// <param name="id">The grader ID (e.g., "exact-match", "llm-judge").</param>
    /// <returns>The grader, or null if not found.</returns>
    public IAiTestGrader? GetById(string id)
    {
        return this.FirstOrDefault(g => g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all graders of a specific type.
    /// </summary>
    /// <param name="type">The grader type to filter by.</param>
    /// <returns>Graders matching the specified type.</returns>
    public IEnumerable<IAiTestGrader> GetByType(GraderType type)
    {
        return this.Where(g => g.Type == type);
    }
}

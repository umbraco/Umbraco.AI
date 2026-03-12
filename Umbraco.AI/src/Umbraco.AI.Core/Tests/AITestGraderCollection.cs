using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// A collection of AI test graders.
/// </summary>
public sealed class AITestGraderCollection : BuilderCollectionBase<IAITestGrader>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestGraderCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the graders.</param>
    public AITestGraderCollection(Func<IEnumerable<IAITestGrader>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a grader by its unique identifier.
    /// </summary>
    /// <param name="graderId">The grader identifier.</param>
    /// <returns>The grader, or <c>null</c> if not found.</returns>
    public IAITestGrader? GetById(string graderId)
        => this.FirstOrDefault(g => g.Id.Equals(graderId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all graders of a specific type.
    /// </summary>
    /// <param name="type">The grader type.</param>
    /// <returns>Graders of the specified type.</returns>
    public IEnumerable<IAITestGrader> GetByType(AIGraderType type)
        => this.Where(g => g.Type == type);
}

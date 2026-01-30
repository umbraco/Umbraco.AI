namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Attribute used to mark and discover grader implementations.
/// Graders are automatically discovered via assembly scanning and registered
/// in the AiTestGraderCollection.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AiTestGraderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiTestGraderAttribute"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this grader (e.g., "exact-match", "llm-judge").</param>
    /// <param name="name">Display name for this grader.</param>
    public AiTestGraderAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Gets the unique identifier for this grader.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name for this grader.
    /// </summary>
    public string Name { get; }
}

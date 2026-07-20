namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// Attribute to mark AI knowledge set implementations for auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIKnowledgeSetAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier of the knowledge set.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the knowledge set.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the description of the knowledge set for the UI.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the Umbraco icon alias for the UI.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIKnowledgeSetAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier (e.g., "umbraco-engage").</param>
    /// <param name="name">The display name (e.g., "Umbraco Engage").</param>
    public AIKnowledgeSetAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

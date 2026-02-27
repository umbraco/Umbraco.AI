namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Attribute to mark AI tool implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIToolAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier of the AI tool.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the AI tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the scope identifier for permission and grouping purposes.
    /// </summary>
    /// <remarks>
    /// Examples: "content-read", "content-write", "media-read", "search"
    /// Defaults to "general" if not specified.
    /// </remarks>
    public string ScopeId { get; set; } = "general";

    /// <summary>
    /// Gets or sets whether the tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Gets or sets tags for additional categorization.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AIToolAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the tool.</param>
    /// <param name="name">The display name of the tool.</param>
    public AIToolAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

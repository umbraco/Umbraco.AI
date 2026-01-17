namespace Umbraco.Ai.Core.Tools;

/// <summary>
/// Attribute to mark AI tool implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiToolAttribute : Attribute
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
    /// Gets or sets the category of the tool for grouping purposes.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets whether the tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Gets or sets tags for additional categorization.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AiToolAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the tool.</param>
    /// <param name="name">The display name of the tool.</param>
    public AiToolAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

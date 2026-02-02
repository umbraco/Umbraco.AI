namespace Umbraco.Ai.Core.Contexts.ResourceTypes;

/// <summary>
/// Attribute to mark AI context resource type implementations for auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiContextResourceTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier of the resource type.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the resource type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResourceTypeAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier (e.g., "brand-voice", "text").</param>
    /// <param name="name">The display name (e.g., "Brand Voice", "Text").</param>
    public AiContextResourceTypeAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Attribute used to mark and discover test feature implementations.
/// Test features are automatically discovered via assembly scanning and registered
/// in the AiTestFeatureCollection.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AiTestFeatureAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiTestFeatureAttribute"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this test feature (e.g., "prompt", "agent").</param>
    /// <param name="name">Display name for this test feature.</param>
    public AiTestFeatureAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Gets the unique identifier for this test feature.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name for this test feature.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the category for grouping test features in UI.
    /// </summary>
    public string Category { get; set; } = "Custom";
}

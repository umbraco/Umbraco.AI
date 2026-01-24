namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Represents a single property change between two versions of an entity.
/// </summary>
public sealed class AiPropertyChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiPropertyChange"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the changed property.</param>
    /// <param name="oldValue">The old value (before the change).</param>
    /// <param name="newValue">The new value (after the change).</param>
    public AiPropertyChange(string propertyName, string? oldValue, string? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the name of the changed property.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the old value of the property (before the change).
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new value of the property (after the change).
    /// </summary>
    public string? NewValue { get; }
}

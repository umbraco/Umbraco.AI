namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Represents a single value change between two versions of an entity.
/// Used for version comparison and diff visualization.
/// </summary>
public sealed class AIValueChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIValueChange"/> class.
    /// </summary>
    /// <param name="path">The JSON path to the changed value.</param>
    /// <param name="oldValue">The old value (before the change).</param>
    /// <param name="newValue">The new value (after the change).</param>
    public AIValueChange(string path, string? oldValue, string? newValue)
    {
        Path = path;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the JSON path to the changed value.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the old value (before the change).
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new value (after the change).
    /// </summary>
    public string? NewValue { get; }
}

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Represents a request to change a value in entity data using a JSON path.
/// Changes are staged in the workspace - user must save to persist.
/// </summary>
public sealed class AIValueChange
{
    /// <summary>
    /// JSON path to the value (e.g., "title", "price.amount", "inventory.quantity").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The new value to set.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// The culture for variant content. Null indicates invariant.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment for segmented content. Null indicates no segment.
    /// </summary>
    public string? Segment { get; init; }
}

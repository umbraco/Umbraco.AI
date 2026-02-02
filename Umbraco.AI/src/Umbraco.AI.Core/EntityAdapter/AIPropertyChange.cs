namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Represents a request to change a property value.
/// Changes are staged in the workspace - user must save to persist.
/// </summary>
public sealed class AiPropertyChange
{
    /// <summary>
    /// The property alias.
    /// </summary>
    public required string Alias { get; init; }

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

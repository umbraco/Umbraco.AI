using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Represents a property change to be applied to an entity.
/// </summary>
public class PropertyChangeModel
{
    /// <summary>
    /// The property alias.
    /// </summary>
    [Required]
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

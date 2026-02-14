using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Represents a value change to be applied to an entity using a JSON path.
/// </summary>
public class ValueChangeModel
{
    /// <summary>
    /// JSON path to the value (e.g., "title", "price.amount", "inventory.quantity").
    /// </summary>
    [Required]
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

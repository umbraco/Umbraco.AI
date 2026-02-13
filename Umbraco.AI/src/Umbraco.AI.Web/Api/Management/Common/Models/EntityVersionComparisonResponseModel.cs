using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Common.Models;

/// <summary>
/// Response model for comparing two versions of an entity.
/// </summary>
public class EntityVersionComparisonResponseModel
{
    /// <summary>
    /// The version number being compared from.
    /// </summary>
    [Required]
    public int FromVersion { get; set; }

    /// <summary>
    /// The version number being compared to.
    /// </summary>
    [Required]
    public int ToVersion { get; set; }

    /// <summary>
    /// The list of value changes between the versions.
    /// </summary>
    [Required]
    public IEnumerable<ValueChangeModel> Changes { get; set; } = [];
}

/// <summary>
/// Represents a single value change between versions.
/// </summary>
public class ValueChangeModel
{
    /// <summary>
    /// The path of the value that changed.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The old value (from the source version).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The new value (from the target version).
    /// </summary>
    public string? NewValue { get; set; }
}

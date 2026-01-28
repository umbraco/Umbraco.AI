using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Common.Models;

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
    /// The list of property changes between the versions.
    /// </summary>
    [Required]
    public IEnumerable<PropertyChangeModel> Changes { get; set; } = [];
}

/// <summary>
/// Represents a single property change between versions.
/// </summary>
public class PropertyChangeModel
{
    /// <summary>
    /// The name of the property that changed.
    /// </summary>
    [Required]
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The old value (from the source version).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The new value (from the target version).
    /// </summary>
    public string? NewValue { get; set; }
}

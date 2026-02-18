using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Information about an available test feature (test type).
/// </summary>
public class TestFeatureInfoModel
{
    /// <summary>
    /// The unique identifier for the test feature.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the test feature.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this test feature does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The category this test feature belongs to (e.g., "Prompt", "Agent", "Custom").
    /// </summary>
    public string? Category { get; set; }
}

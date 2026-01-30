using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Information about a registered test feature.
/// </summary>
public class TestFeatureInfoModel
{
    /// <summary>
    /// The test feature ID (e.g., "prompt", "agent").
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the test feature.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this test feature does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The category of the test feature.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether this test feature has a test case schema.
    /// </summary>
    public bool HasTestCaseSchema { get; set; }
}

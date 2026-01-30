using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Information about a registered test grader.
/// </summary>
public class TestGraderInfoModel
{
    /// <summary>
    /// The grader type ID (e.g., "exact-match", "llm-judge").
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the grader.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The grader type (0=CodeBased, 1=ModelBased, 2=Human).
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// Whether this grader has a configuration schema.
    /// </summary>
    public bool HasConfigSchema { get; set; }
}

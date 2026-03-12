using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Information about an available test grader.
/// </summary>
public class TestGraderInfoModel
{
    /// <summary>
    /// The unique identifier for the grader.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the grader.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this grader evaluates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of grader (CodeBased, ModelBased, Human).
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;
}

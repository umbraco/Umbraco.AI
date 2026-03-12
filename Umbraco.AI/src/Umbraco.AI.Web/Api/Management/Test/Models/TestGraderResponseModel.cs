using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Full response model for a test grader including configuration schema.
/// </summary>
public class TestGraderResponseModel
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

    /// <summary>
    /// The schema for the grader configuration.
    /// Null if the grader does not require configuration.
    /// </summary>
    public EditableModelSchemaModel? ConfigSchema { get; set; }
}

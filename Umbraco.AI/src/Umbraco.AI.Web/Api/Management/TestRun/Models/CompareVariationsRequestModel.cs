using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.TestRun.Models;

/// <summary>
/// Request model for comparing two variation groups within an execution.
/// </summary>
public class CompareVariationsRequestModel
{
    /// <summary>
    /// The execution ID containing both variations.
    /// </summary>
    [Required]
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// The source variation ID (null for default config).
    /// </summary>
    public Guid? SourceVariationId { get; set; }

    /// <summary>
    /// The comparison variation ID.
    /// </summary>
    [Required]
    public Guid ComparisonVariationId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.TestRun.Models;

/// <summary>
/// Request model for comparing two test runs.
/// </summary>
public class CompareRunsRequestModel
{
    /// <summary>
    /// The baseline run ID.
    /// </summary>
    [Required]
    public Guid BaselineRunId { get; set; }

    /// <summary>
    /// The comparison run ID.
    /// </summary>
    [Required]
    public Guid ComparisonRunId { get; set; }
}

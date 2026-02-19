using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.TestRun.Models;

/// <summary>
/// Request model for comparing two test runs.
/// </summary>
public class CompareRunsRequestModel
{
    /// <summary>
    /// The baseline test run ID.
    /// </summary>
    [Required]
    public Guid BaselineTestRunId { get; set; }

    /// <summary>
    /// The comparison test run ID.
    /// </summary>
    [Required]
    public Guid ComparisonTestRunId { get; set; }
}

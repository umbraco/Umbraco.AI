using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for test metrics (pass@k, pass^k).
/// </summary>
public class TestMetricsResponseModel
{
    /// <summary>
    /// The test ID these metrics belong to.
    /// </summary>
    [Required]
    public Guid TestId { get; set; }

    /// <summary>
    /// Total number of runs executed.
    /// </summary>
    public int TotalRuns { get; set; }

    /// <summary>
    /// Number of runs that passed (at least one grader passed).
    /// </summary>
    public int PassedRuns { get; set; }

    /// <summary>
    /// pass@k metric: probability of at least one success (0-1).
    /// Calculated as: (runs with >=1 passed grader) / total runs.
    /// </summary>
    public float PassAtK { get; set; }

    /// <summary>
    /// pass^k metric: probability that all runs succeed (0-1).
    /// Calculated as: (runs with all graders passed) / total runs.
    /// </summary>
    public float PassToTheK { get; set; }

    /// <summary>
    /// The run IDs included in these metrics.
    /// </summary>
    public IReadOnlyList<Guid> RunIds { get; set; } = [];
}

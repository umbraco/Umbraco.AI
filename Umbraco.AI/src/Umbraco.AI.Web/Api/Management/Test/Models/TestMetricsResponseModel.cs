using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for test execution metrics.
/// Contains pass@k and pass^k metrics calculated from multiple test runs.
/// </summary>
public class TestMetricsResponseModel
{
    /// <summary>
    /// The test ID these metrics are for.
    /// </summary>
    [Required]
    public Guid TestId { get; set; }

    /// <summary>
    /// Total number of runs executed.
    /// </summary>
    public int TotalRuns { get; set; }

    /// <summary>
    /// Number of runs that passed (all Error-severity graders passed).
    /// </summary>
    public int PassedRuns { get; set; }

    /// <summary>
    /// pass@k - Probability that at least one run succeeds.
    /// Formula: PassedRuns / TotalRuns
    /// Example: 2/3 = 0.67 (67% chance of at least one success)
    /// </summary>
    public double PassAtK { get; set; }

    /// <summary>
    /// pass^k - Probability that all runs succeed.
    /// Formula: PassedRuns == TotalRuns ? 1.0 : 0.0
    /// Example: All passed = 1.0, any failed = 0.0
    /// </summary>
    public double PassToTheK { get; set; }

    /// <summary>
    /// IDs of the runs included in this metric calculation.
    /// Use these to retrieve individual run details via the test run endpoints.
    /// </summary>
    public IReadOnlyList<Guid> RunIds { get; set; } = [];
}

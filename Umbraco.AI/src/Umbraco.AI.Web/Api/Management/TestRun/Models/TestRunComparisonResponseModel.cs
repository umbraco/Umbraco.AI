using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Test.Models;

namespace Umbraco.AI.Web.Api.Management.TestRun.Models;

/// <summary>
/// Response model for test run comparison.
/// </summary>
public class TestRunComparisonResponseModel
{
    /// <summary>
    /// The baseline run.
    /// </summary>
    [Required]
    public TestRunResponseModel BaselineRun { get; set; } = null!;

    /// <summary>
    /// The comparison run.
    /// </summary>
    [Required]
    public TestRunResponseModel ComparisonRun { get; set; } = null!;

    /// <summary>
    /// Whether the comparison run represents a regression from the baseline.
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Whether the comparison run represents an improvement from the baseline.
    /// </summary>
    public bool IsImprovement { get; set; }

    /// <summary>
    /// Change in duration (positive = slower, negative = faster).
    /// </summary>
    public long DurationChangeMs { get; set; }

    /// <summary>
    /// Grader-level comparison results.
    /// </summary>
    public IReadOnlyList<TestGraderComparisonResponseModel> GraderComparisons { get; set; } = [];
}

/// <summary>
/// Response model for grader-level comparison.
/// </summary>
public class TestGraderComparisonResponseModel
{
    /// <summary>
    /// The grader ID.
    /// </summary>
    [Required]
    public Guid GraderId { get; set; }

    /// <summary>
    /// The grader name.
    /// </summary>
    [Required]
    public string GraderName { get; set; } = string.Empty;

    /// <summary>
    /// Baseline grader result.
    /// </summary>
    public TestGraderResultResponseModel? BaselineResult { get; set; }

    /// <summary>
    /// Comparison grader result.
    /// </summary>
    public TestGraderResultResponseModel? ComparisonResult { get; set; }

    /// <summary>
    /// Whether this grader result changed between runs.
    /// </summary>
    public bool Changed { get; set; }

    /// <summary>
    /// Score change (positive = improvement, negative = regression).
    /// </summary>
    public double ScoreChange { get; set; }
}

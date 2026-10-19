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
    /// Change in overall run duration, including grading (positive = slower, negative = faster).
    /// For the change in AI call time alone, see <see cref="TestUsageComparisonResponseModel.CallDurationChangeMs"/>.
    /// </summary>
    public long DurationChangeMs { get; set; }

    /// <summary>
    /// Grader-level comparison results.
    /// </summary>
    public IReadOnlyList<TestGraderComparisonResponseModel> GraderComparisons { get; set; } = [];

    /// <summary>
    /// How the AI usage changed between the runs. Null when either run has no usage
    /// (saved before usage was recorded, or made no tracked AI call).
    /// </summary>
    public TestUsageComparisonResponseModel? UsageComparison { get; set; }
}

/// <summary>
/// Response model for how the AI usage changed between two runs. Every change is comparison minus baseline.
/// Covers tracked AI calls made by the test feature; grader calls are excluded.
/// </summary>
public class TestUsageComparisonResponseModel
{
    /// <summary>
    /// Change in input tokens.
    /// </summary>
    public int InputTokensChange { get; set; }

    /// <summary>
    /// Change in output tokens.
    /// </summary>
    public int OutputTokensChange { get; set; }

    /// <summary>
    /// Change in total tokens.
    /// </summary>
    public int TotalTokensChange { get; set; }

    /// <summary>
    /// Change in the number of tracked AI calls.
    /// </summary>
    public int CallCountChange { get; set; }

    /// <summary>
    /// Change in the number of failed AI calls.
    /// </summary>
    public int FailedCallCountChange { get; set; }

    /// <summary>
    /// Change in summed AI call time, in milliseconds. Excludes grading; overlapping calls are summed.
    /// </summary>
    public long CallDurationChangeMs { get; set; }

    /// <summary>
    /// Whether either run had calls that reported no usage, making the token changes approximate.
    /// </summary>
    public bool HasUnreportedCalls { get; set; }

    /// <summary>
    /// Whether the runs used a different set of capabilities, providers, models, profiles or features.
    /// </summary>
    public bool BreakdownChanged { get; set; }

    /// <summary>
    /// Per-entry comparison, one item per capability, provider, model, profile and feature combination in either run.
    /// </summary>
    public IReadOnlyList<TestUsageEntryComparisonResponseModel> Entries { get; set; } = [];
}

/// <summary>
/// Response model for one usage breakdown entry compared between two runs. Either side is null when the
/// combination only appears in the other run.
/// </summary>
public class TestUsageEntryComparisonResponseModel
{
    /// <summary>
    /// The capability the calls were made with.
    /// </summary>
    [Required]
    public string Capability { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the provider that served the calls, if known.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// The ID of the model that served the calls, if known.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// The ID of the profile the calls were made through, if any.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The alias of the profile the calls were made through, if any.
    /// </summary>
    public string? ProfileAlias { get; set; }

    /// <summary>
    /// The type of feature that made the calls, if any.
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// The ID of the feature that made the calls, if any.
    /// </summary>
    public Guid? FeatureId { get; set; }

    /// <summary>
    /// The alias of the feature that made the calls, if any.
    /// </summary>
    public string? FeatureAlias { get; set; }

    /// <summary>
    /// The entry in the baseline run, or null when only the comparison run has it.
    /// </summary>
    public TestUsageEntryResponseModel? BaselineEntry { get; set; }

    /// <summary>
    /// The entry in the comparison run, or null when only the baseline run has it.
    /// </summary>
    public TestUsageEntryResponseModel? ComparisonEntry { get; set; }

    /// <summary>
    /// Change in total tokens for this entry. A missing side counts as zero.
    /// </summary>
    public int TotalTokensChange { get; set; }

    /// <summary>
    /// Change in summed AI call time for this entry, in milliseconds. A missing side counts as zero.
    /// </summary>
    public long CallDurationChangeMs { get; set; }

    /// <summary>
    /// Change in failed calls for this entry. A missing side counts as zero.
    /// </summary>
    public int FailedCallCountChange { get; set; }
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

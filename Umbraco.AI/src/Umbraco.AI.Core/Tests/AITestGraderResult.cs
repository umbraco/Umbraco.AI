namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Result of applying a single grader to a test run.
/// </summary>
public sealed class AITestGraderResult
{
    /// <summary>
    /// The ID of the grader that produced this result.
    /// </summary>
    public required Guid GraderId { get; set; }

    /// <summary>
    /// Whether the grader passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Score from the grader (0-1).
    /// - Code-based graders return 0 or 1
    /// - Model-based graders return continuous scores
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// The actual value that was evaluated.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// The expected value (if applicable).
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// Failure message explaining why the grader failed (if it failed).
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Optional metadata from the grader (JSON).
    /// Can store additional analysis data.
    /// </summary>
    public string? MetadataJson { get; set; }
}

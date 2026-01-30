namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Represents the result of a grader evaluation for a specific run.
/// </summary>
public sealed class AiTestGraderResult
{
    /// <summary>
    /// The unique identifier of the grader result.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The ID of the run this result belongs to.
    /// </summary>
    public Guid RunId { get; internal set; }

    /// <summary>
    /// The ID of the grader that produced this result.
    /// </summary>
    public Guid GraderId { get; internal set; }

    /// <summary>
    /// Whether the grader passed or failed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// The score from the grader (0.0 to 1.0) for model-based graders.
    /// Null for binary code-based graders.
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// The actual value that was evaluated.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// The expected value for comparison graders.
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// The failure message if the grader failed.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }
}

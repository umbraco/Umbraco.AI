namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core entity representing a grader result.
/// </summary>
internal class AiTestGraderResultEntity
{
    /// <summary>
    /// Unique identifier for the grader result.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the run.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Foreign key to the grader.
    /// </summary>
    public Guid GraderId { get; set; }

    /// <summary>
    /// Whether the grader passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// The score from the grader (0.0 to 1.0), nullable for binary graders.
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// The actual value that was evaluated.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// The expected value for comparison.
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// The failure message if failed.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Navigation property to run.
    /// </summary>
    public AiTestRunEntity Run { get; set; } = null!;

    /// <summary>
    /// Navigation property to grader.
    /// </summary>
    public AiTestGraderEntity Grader { get; set; } = null!;
}

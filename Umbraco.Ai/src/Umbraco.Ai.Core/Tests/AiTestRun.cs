namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Represents a single execution of a test.
/// Multiple runs can exist for a test (RunNumber 1 to N based on test's RunCount).
/// </summary>
public sealed class AiTestRun
{
    /// <summary>
    /// The unique identifier of the run.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The ID of the test that was executed.
    /// </summary>
    public Guid TestId { get; internal set; }

    /// <summary>
    /// The version of the test that was executed.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// The run number (1 to N) within the test execution.
    /// </summary>
    public int RunNumber { get; set; }

    /// <summary>
    /// The profile ID that was used for this run (important for profile override comparisons).
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The context IDs that were used for this run (important for context override comparisons).
    /// Serialized as JSON array.
    /// </summary>
    public string? ContextIdsJson { get; set; }

    /// <summary>
    /// When the run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who executed this run.
    /// </summary>
    public Guid? ExecutedByUserId { get; set; }

    /// <summary>
    /// The status of the run.
    /// </summary>
    public AiTestRunStatus Status { get; set; }

    /// <summary>
    /// The duration of the run in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Error message if the run failed with an error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The final outcome produced by the test execution.
    /// </summary>
    public AiTestOutcome? Outcome { get; set; }

    /// <summary>
    /// The associated transcript with detailed execution trace.
    /// </summary>
    public AiTestTranscript? Transcript { get; set; }

    /// <summary>
    /// The grader results for this run.
    /// </summary>
    public IReadOnlyList<AiTestGraderResult> GraderResults { get; set; } = Array.Empty<AiTestGraderResult>();

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Optional batch ID if this run was part of a batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }
}

/// <summary>
/// Status of a test run.
/// </summary>
public enum AiTestRunStatus
{
    /// <summary>
    /// The run passed all graders.
    /// </summary>
    Passed = 0,

    /// <summary>
    /// The run failed one or more graders.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// The run encountered an error during execution.
    /// </summary>
    Error = 2
}

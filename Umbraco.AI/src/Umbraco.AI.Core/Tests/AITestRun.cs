namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Represents a single execution of a test.
/// Each test can have multiple runs (RunCount) to measure non-deterministic behavior.
/// </summary>
public sealed class AITestRun
{
    /// <summary>
    /// Unique identifier for this run.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the test that was run.
    /// </summary>
    public required Guid TestId { get; set; }

    /// <summary>
    /// The version of the test that was run.
    /// Allows historical analysis of test results across test definition changes.
    /// </summary>
    public int TestVersion { get; set; }

    /// <summary>
    /// The run number within a batch (1 to RunCount).
    /// </summary>
    public int RunNumber { get; set; } = 1;

    /// <summary>
    /// The profile ID used for this run.
    /// May differ from the target's default profile if profileIdOverride was provided.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The context IDs used for this run.
    /// May differ from the target's default contexts if contextIdsOverride was provided.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// When this run was executed.
    /// </summary>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who executed this run.
    /// </summary>
    public Guid? ExecutedByUserId { get; set; }

    /// <summary>
    /// Overall status of the run.
    /// </summary>
    public AITestRunStatus Status { get; set; } = AITestRunStatus.Running;

    /// <summary>
    /// Duration of execution in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// The transcript ID containing execution details.
    /// </summary>
    public Guid? TranscriptId { get; set; }

    /// <summary>
    /// The final outcome of the test execution.
    /// </summary>
    public AITestOutcome? Outcome { get; set; }

    /// <summary>
    /// Grading results for each grader.
    /// </summary>
    public IReadOnlyList<AITestGraderResult> GraderResults { get; set; } = Array.Empty<AITestGraderResult>();

    /// <summary>
    /// Optional metadata for this run (JSON).
    /// Can store custom data for analysis.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Optional batch ID if this run was part of a batch execution.
    /// All runs in the same batch share this ID.
    /// </summary>
    public Guid? BatchId { get; set; }
}

/// <summary>
/// Status of a test run.
/// </summary>
public enum AITestRunStatus
{
    /// <summary>
    /// Run is currently executing.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Run completed and all graders passed.
    /// </summary>
    Passed = 1,

    /// <summary>
    /// Run completed but one or more graders failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Run encountered an error during execution.
    /// </summary>
    Error = 3
}
